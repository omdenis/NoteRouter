namespace NoteRouter.Services;

public interface ITranscriptionService
{
    Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default);
}
