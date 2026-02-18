using Telegram.Bot.Types;

namespace NoteRouter.Services;

public interface ITelegramService
{
    Task<IEnumerable<Message>> GetNewMessagesAsync(CancellationToken cancellationToken = default);
    Task<string> DownloadMediaAsync(Message message, string destinationPath, CancellationToken cancellationToken = default);
    Task SendReplyAsync(long chatId, int messageId, string text, CancellationToken cancellationToken = default);
}
