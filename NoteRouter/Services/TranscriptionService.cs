using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoteRouter.Configuration;
using Whisper.net;

namespace NoteRouter.Services;

public class TranscriptionService : ITranscriptionService, IDisposable
{
    private readonly WhisperProcessor _processor;
    private readonly ILogger<TranscriptionService> _logger;

    public TranscriptionService(IOptions<AppSettings> settings, ILogger<TranscriptionService> logger)
    {
        _logger = logger;
        var modelPath = settings.Value.WhisperModelPath;

        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Whisper model not found at: {modelPath}");

        var factory = WhisperFactory.FromPath(modelPath);
        _processor = factory.CreateBuilder()
            .WithLanguage("auto")
            .Build();
    }

    public async Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting transcription for {FilePath}", audioFilePath);

        await using var fileStream = File.OpenRead(audioFilePath);
        var segments = new List<string>();

        await foreach (var segment in _processor.ProcessAsync(fileStream, cancellationToken))
        {
            segments.Add(segment.Text);
        }

        var result = string.Join(" ", segments).Trim();
        _logger.LogInformation("Transcription completed: {Length} characters", result.Length);

        return result;
    }

    public void Dispose()
    {
        _processor.Dispose();
    }
}
