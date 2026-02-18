namespace NoteRouter.Configuration;

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public string BotKey { get; set; } = string.Empty;
    public long[] ChatIds { get; set; } = [];
    public string ObsidianPath { get; set; } = string.Empty;
    public string MediaPath { get; set; } = string.Empty;
    public string DailyNotePath { get; set; } = string.Empty;
    public string WhisperModelPath { get; set; } = string.Empty;
}
