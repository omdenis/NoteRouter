using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoteRouter.Configuration;

namespace NoteRouter.Services;

public class ObsidianService : IObsidianService
{
    private readonly AppSettings _settings;
    private readonly ILogger<ObsidianService> _logger;

    public ObsidianService(IOptions<AppSettings> settings, ILogger<ObsidianService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<(string Path, Guid Guid)> CreateNoteAsync(string content, long chatId, string? messageUrl, CancellationToken cancellationToken = default)
    {
        var guid = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var fileName = $"{now:yyyy-MM-dd}_{now:HHmmss}_{chatId}.md";
        var inboxPath = Path.Combine(_settings.ObsidianPath, "inbox");
        var fullPath = Path.Combine(inboxPath, fileName);

        Directory.CreateDirectory(inboxPath);

        var noteContent = new StringBuilder();
        noteContent.AppendLine("---");
        noteContent.AppendLine($"created: {now:yyyy-MM-dd}");
        noteContent.AppendLine($"id: {guid}");
        if (!string.IsNullOrEmpty(messageUrl))
            noteContent.AppendLine($"url: {messageUrl}");
        noteContent.AppendLine("---");
        noteContent.AppendLine();
        noteContent.AppendLine(content);

        await File.WriteAllTextAsync(fullPath, noteContent.ToString(), cancellationToken);

        _logger.LogInformation("Created note at {Path} with GUID {Guid}", fullPath, guid);
        return (fullPath, guid);
    }

    public string GetObsidianUri(string notePath, Guid guid)
    {
        var vaultName = Path.GetFileName(_settings.ObsidianPath);
        var relativePath = Path.GetRelativePath(_settings.ObsidianPath, notePath);
        var encodedPath = Uri.EscapeDataString(relativePath.Replace(Path.DirectorySeparatorChar, '/'));

        return $"obsidian://open?vault={Uri.EscapeDataString(vaultName)}&file={encodedPath}";
    }
}
