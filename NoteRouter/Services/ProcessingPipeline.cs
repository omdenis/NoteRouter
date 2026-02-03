using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoteRouter.Configuration;
using NoteRouter.Data;
using NoteRouter.Data.Enums;
using TelegramMessage = Telegram.Bot.Types.Message;
using Message = NoteRouter.Data.Entities.Message;

namespace NoteRouter.Services;

public class ProcessingPipeline : IProcessingPipeline
{
    private readonly AppDbContext _dbContext;
    private readonly ITelegramService _telegramService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly IObsidianService _obsidianService;
    private readonly AppSettings _settings;
    private readonly ILogger<ProcessingPipeline> _logger;

    public ProcessingPipeline(
        AppDbContext dbContext,
        ITelegramService telegramService,
        ITranscriptionService transcriptionService,
        IObsidianService obsidianService,
        IOptions<AppSettings> settings,
        ILogger<ProcessingPipeline> logger)
    {
        _dbContext = dbContext;
        _telegramService = telegramService;
        _transcriptionService = transcriptionService;
        _obsidianService = obsidianService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ProcessAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting processing pipeline");

        // Phase 1: Fetch new messages from Telegram
        await FetchNewMessagesAsync(cancellationToken);

        // Phase 2: Process unprocessed messages
        await ProcessUnprocessedMessagesAsync(cancellationToken);

        _logger.LogInformation("Processing pipeline completed");
    }

    private async Task FetchNewMessagesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching new messages from Telegram");

        var telegramMessages = await _telegramService.GetNewMessagesAsync(cancellationToken);

        foreach (var tgMessage in telegramMessages)
        {
            var exists = await _dbContext.Messages
                .AnyAsync(m => m.ChatId == tgMessage.Chat.Id && m.MessageId == tgMessage.MessageId, cancellationToken);

            if (exists)
                continue;

            var message = new Message
            {
                MessageId = tgMessage.MessageId,
                ChatId = tgMessage.Chat.Id,
                MessageUrl = GetMessageUrl(tgMessage),
                Status = MessageStatus.New,
                Guid = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Messages.Add(message);
            _logger.LogInformation("Added new message {MessageId} from chat {ChatId}", tgMessage.MessageId, tgMessage.Chat.Id);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessUnprocessedMessagesAsync(CancellationToken cancellationToken)
    {
        var unprocessedMessages = await _dbContext.Messages
            .Where(m => m.Status != MessageStatus.Done)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Processing {Count} unprocessed messages", unprocessedMessages.Count);

        foreach (var message in unprocessedMessages)
        {
            try
            {
                await ProcessMessageAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message {MessageId} from chat {ChatId}", message.MessageId, message.ChatId);
                message.Status = MessageStatus.Failed;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing message {MessageId} from chat {ChatId}", message.MessageId, message.ChatId);

        // Update status to InProgress
        message.Status = MessageStatus.InProgress;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get the original Telegram message to download media
        var telegramMessages = await _telegramService.GetNewMessagesAsync(cancellationToken);
        var tgMessage = telegramMessages.FirstOrDefault(m => m.MessageId == message.MessageId && m.Chat.Id == message.ChatId);

        string transcribedText;
        if (tgMessage is not null && HasMedia(tgMessage))
        {
            // Download media
            var mediaPath = await _telegramService.DownloadMediaAsync(tgMessage, _settings.MediaPath, cancellationToken);
            message.MediaPath = mediaPath;

            // Transcribe audio
            transcribedText = await _transcriptionService.TranscribeAsync(mediaPath, cancellationToken);
        }
        else
        {
            // Use message text if no media
            transcribedText = tgMessage?.Text ?? tgMessage?.Caption ?? "[No content]";
        }

        // Create Obsidian note
        var (notePath, noteGuid) = await _obsidianService.CreateNoteAsync(
            transcribedText,
            message.ChatId,
            message.MessageUrl,
            cancellationToken);

        message.Path = notePath;
        message.Guid = noteGuid;
        message.Status = MessageStatus.Done;
        message.ProcessedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send reply to Telegram
        var obsidianUri = _obsidianService.GetObsidianUri(notePath, noteGuid);
        var replyText = $"Note saved!\nGUID: {noteGuid}\nLink: {obsidianUri}";

        await _telegramService.SendReplyAsync(message.ChatId, (int)message.MessageId, replyText, cancellationToken);

        _logger.LogInformation("Successfully processed message {MessageId}", message.MessageId);
    }

    private static string? GetMessageUrl(TelegramMessage message)
    {
        if (message.Chat.Username is not null)
            return $"https://t.me/{message.Chat.Username}/{message.MessageId}";

        return $"https://t.me/c/{message.Chat.Id.ToString().TrimStart('-')}/{message.MessageId}";
    }

    private static bool HasMedia(TelegramMessage message)
    {
        return message.Voice is not null
            || message.Audio is not null
            || message.Video is not null
            || message.VideoNote is not null
            || message.Document is not null;
    }
}
