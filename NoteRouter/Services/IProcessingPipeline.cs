namespace NoteRouter.Services;

public interface IProcessingPipeline
{
    Task ProcessAllAsync(CancellationToken cancellationToken = default);
}
