using Microsoft.EntityFrameworkCore;
using NoteRouter.Data.Entities;

namespace NoteRouter.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ChatId, e.MessageId }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Guid).HasDefaultValueSql("(lower(hex(randomblob(16))))");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
        });
    }
}
