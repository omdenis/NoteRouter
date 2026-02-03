using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoteRouter.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NoteRouter.Services;

public class TelegramService : ITelegramService
{
    private readonly TelegramBotClient _client;
    private readonly AppSettings _settings;
    private readonly ILogger<TelegramService> _logger;
    private readonly HashSet<long> _allowedChatIds;

    public TelegramService(IOptions<AppSettings> settings, ILogger<TelegramService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var botToken = Environment.GetEnvironmentVariable(_settings.BotKey)
            ?? throw new InvalidOperationException($"Environment variable '{_settings.BotKey}' not found");

        _client = new TelegramBotClient(botToken);
        _allowedChatIds = [.. _settings.ChatIds];
    }

    public async Task<IEnumerable<Message>> GetNewMessagesAsync(CancellationToken cancellationToken = default)
    {
        var messages = new List<Message>();
        var offset = 0;

        while (true)
        {
            var updates = await _client.GetUpdates(
                offset: offset,
                allowedUpdates: [UpdateType.Message],
                cancellationToken: cancellationToken);

            if (updates.Length == 0)
                break;

            foreach (var update in updates)
            {
                if (update.Message is { } message && _allowedChatIds.Contains(message.Chat.Id))
                {
                    messages.Add(message);
                    _logger.LogInformation("Received message {MessageId} from chat {ChatId}", message.MessageId, message.Chat.Id);
                }

                offset = update.Id + 1;
            }
        }

        return messages;
    }

    public async Task<string> DownloadMediaAsync(Message message, string destinationPath, CancellationToken cancellationToken = default)
    {
        var fileId = GetFileId(message);
        if (fileId is null)
            throw new InvalidOperationException("Message does not contain downloadable media");

        var file = await _client.GetFile(fileId, cancellationToken);
        if (file.FilePath is null)
            throw new InvalidOperationException("Could not get file path from Telegram");

        var extension = Path.GetExtension(file.FilePath) ?? ".bin";
        var fileName = $"{DateTime.UtcNow:yyyy-MM-dd}-{message.Chat.Id}-{message.MessageId}{extension}";
        var fullPath = Path.Combine(destinationPath, fileName);

        Directory.CreateDirectory(destinationPath);

        await using var fileStream = System.IO.File.Create(fullPath);
        await _client.DownloadFile(file.FilePath, fileStream, cancellationToken);

        _logger.LogInformation("Downloaded media to {FilePath}", fullPath);
        return fullPath;
    }

    public async Task SendReplyAsync(long chatId, int messageId, string text, CancellationToken cancellationToken = default)
    {
        await _client.SendMessage(
            chatId: chatId,
            text: text,
            replyParameters: new ReplyParameters { MessageId = messageId },
            cancellationToken: cancellationToken);

        _logger.LogInformation("Sent reply to message {MessageId} in chat {ChatId}", messageId, chatId);
    }

    private static string? GetFileId(Message message)
    {
        return message.Voice?.FileId
            ?? message.Audio?.FileId
            ?? message.Video?.FileId
            ?? message.VideoNote?.FileId
            ?? message.Document?.FileId;
    }
}
