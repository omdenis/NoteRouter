using NoteRouter.Data.Enums;

namespace NoteRouter.Data.Entities;

public class Message
{
    public int Id { get; set; }
    public long MessageId { get; set; }
    public long ChatId { get; set; }
    public string? MessageUrl { get; set; }
    public string? MediaPath { get; set; }
    public MessageStatus Status { get; set; }
    public string? Path { get; set; }
    public Guid Guid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
