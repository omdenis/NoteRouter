namespace NoteRouter.Services;

public interface IObsidianService
{
    Task<(string Path, Guid Guid)> CreateNoteAsync(string content, long chatId, string? messageUrl, CancellationToken cancellationToken = default);
    string GetObsidianUri(string notePath, Guid guid);
}
