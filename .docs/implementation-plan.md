# NoteRouter Implementation Plan

## Project Overview

NoteRouter is a C# .NET 10 Telegram bot that automatically processes messages from specified Telegram groups, downloads media files, transcribes audio to text using Whisper, and saves the results as Obsidian notes.

---

## Technology Stack

- **Runtime**: .NET 10
- **Language**: C# 14
- **Database**: SQLite with Entity Framework Core
- **Migrations**: EF Core Migrations (mandatory)
- **Telegram**: Telegram.Bot library
- **Transcription**: Whisper.net (local Whisper inference)

---

## Architecture

```
┌─────────────────┐     ┌──────────────┐     ┌─────────────────┐
│  Telegram API   │────▶│  NoteRouter  │────▶│  Obsidian Vault │
└─────────────────┘     └──────────────┘     └─────────────────┘
                              │
                              ▼
                        ┌──────────────┐
                        │  SQLite DB   │
                        │  (EF Core)   │
                        └──────────────┘
```

---

## Components

### 1. Configuration Module
- Load configuration from `appsettings.json` and environment variables
- Configuration properties:
  - `BotKey` - Telegram bot token (from environment variable)
  - `ChatIds` - Array of allowed chat IDs
  - `ObsidianPath` - Path to Obsidian vault
  - `MediaPath` - Path to store downloaded media
  - `DailyNotePath` - Template path for daily notes (e.g., `/path/{YYYY}/{MM}/{YYYY-MM-DD}.md`)
  - `WhisperModelPath` - Path to Whisper model file

### 2. Database Layer (Entity Framework Core)
- SQLite database with EF Core
- Migrations are mandatory for schema management
- Entity: `Message`
  - `Id` - Primary key (int, auto-increment)
  - `TelegramMessageId` - Telegram message ID (long)
  - `ChatId` - Telegram chat ID (long)
  - `MessageUrl` - URL to the original Telegram message
  - `Status` - Processing status enum (`New`, `InProgress`, `Done`, `Failed`)
  - `DocumentPath` - Path to created Obsidian note
  - `Guid` - Unique identifier for the note
  - `CreatedAt` - Timestamp
  - `ProcessedAt` - Processing completion timestamp

### 3. Telegram Service
- Connect to Telegram API using Telegram.Bot
- Fetch new messages from allowed chats only
- Download media files (audio, voice, video, documents)
- Send reply messages with processing results

### 4. Media Downloader
- Download files to `MediaPath`
- Filename format: `{datetime:yyyyMMdd_HHmmss}_{chatId}_{messageId}.{ext}`
- Support for various media types

### 5. Transcription Service
- Audio-to-text transcription using Whisper.net
- Local inference with downloaded Whisper model
- Support for multiple audio formats

### 6. Obsidian Note Generator
- Create markdown notes in `ObsidianPath/inbox`
- Filename format: `{date}_{time}_{chatId}.md`
- YAML frontmatter:
  ```yaml
  ---
  created: YYYY-MM-DD
  id: {GUID}
  url: {telegram_message_url}
  ---
  ```

### 7. Processing Pipeline
- Orchestrates the entire flow
- Handles status transitions
- Error handling and retry logic

---

## Implementation Tasks

### Phase 1: Project Setup
- [ ] Create .NET 10 console application
- [ ] Set up solution structure
- [ ] Configure `appsettings.json` and user secrets
- [ ] Add NuGet packages
- [ ] Set up dependency injection

### Phase 2: Database with EF Core
- [ ] Create `AppDbContext`
- [ ] Define `Message` entity
- [ ] Define `MessageStatus` enum
- [ ] Create initial migration
- [ ] Implement repository pattern (mandaroy)
- [ ] Add migration on startup

### Phase 3: Configuration
- [ ] Create `AppSettings` class
- [ ] Implement `IOptions<AppSettings>` pattern
- [ ] Validate configuration on startup
- [ ] Support environment variable overrides

### Phase 4: Telegram Integration
- [ ] Set up `TelegramBotClient`
- [ ] Implement message fetching service
- [ ] Implement chat ID whitelist validation
- [ ] Implement media file downloading
- [ ] Implement reply message sending

### Phase 5: Media Processing
- [ ] Implement file download to MediaPath
- [ ] Generate unique filenames - YYYY-MM-DD-chat_id-message_id
- [ ] Handle different media types (audio, voice, video, images, documents)
- [ ] File type detection

### Phase 6: Whisper Transcription
- [ ] Integrate Whisper.net
- [ ] Download/configure Whisper model
- [ ] Implement audio transcription service
- [ ] Handle multiple audio formats (convert if needed)
- [ ] Error handling for failed transcriptions

### Phase 7: Obsidian Integration
- [ ] Create note generator service
- [ ] Implement YAML frontmatter generation
- [ ] Generate GUIDs for notes
- [ ] Create Obsidian URI links
- [ ] Save notes to inbox folder

### Phase 8: Processing Pipeline
- [ ] Implement main processing pipeline
- [ ] Status management (New → InProgress → Done/Failed)
- [ ] Transaction handling
- [ ] Error handling and logging
- [ ] Telegram reply with Obsidian link and GUID

### Phase 9: Deployment
- [ ] Create hosted service for scheduling
- [ ] Document systemd/cron setup for daily execution
- [ ] Add Serilog logging
- [ ] Create example configuration
- [ ] Write README

---

## Project Structure

```
NoteRouter/
├── src/
│   └── NoteRouter/
│       ├── Program.cs
│       ├── NoteRouter.csproj
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       │
│       ├── Configuration/
│       │   └── AppSettings.cs
│       │
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   ├── Entities/
│       │   │   └── Message.cs
│       │   ├── Enums/
│       │   │   └── MessageStatus.cs
│       │   └── Migrations/
│       │       └── (EF Core migrations)
│       │
│       ├── Services/
│       │   ├── ITelegramService.cs
│       │   ├── TelegramService.cs
│       │   ├── IMediaDownloader.cs
│       │   ├── MediaDownloader.cs
│       │   ├── ITranscriptionService.cs
│       │   ├── TranscriptionService.cs
│       │   ├── IObsidianService.cs
│       │   ├── ObsidianService.cs
│       │   ├── IProcessingPipeline.cs
│       │   └── ProcessingPipeline.cs
│       │
│       └── Workers/
│           └── ProcessingWorker.cs
│
├── tests/
│   └── NoteRouter.Tests/
│       └── NoteRouter.Tests.csproj
│
├── docs/
├── NoteRouter.sln
└── README.md
```

---

## NuGet Packages

```xml
<!-- Telegram -->
<PackageReference Include="Telegram.Bot" Version="22.*" />

<!-- Entity Framework Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.*" />

<!-- Whisper -->
<PackageReference Include="Whisper.net" Version="1.*" />
<PackageReference Include="Whisper.net.Runtime" Version="1.*" />

<!-- Configuration & DI -->
<PackageReference Include="Microsoft.Extensions.Hosting" Version="10.*" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="10.*" />
<PackageReference Include="Microsoft.Extensions.Options" Version="10.*" />

<!-- Logging -->
<PackageReference Include="Serilog" Version="4.*" />
<PackageReference Include="Serilog.Extensions.Hosting" Version="8.*" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.*" />
<PackageReference Include="Serilog.Sinks.File" Version="6.*" />
```

---

## Database Schema

### Message Entity

```csharp
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

public enum MessageStatus
{
    New = 0,
    InProgress = 1,
    Done = 2,
    Failed = 3
}
```

### Migration Commands

```bash
# Create migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script
```

---

## Processing Flow

```
1. START (scheduled daily or via hosted service)
   │
2. ├─▶ Apply pending EF Core migrations
   │
3. ├─▶ Connect to Telegram
   │
4. ├─▶ Fetch messages from allowed ChatIds
   │    └─▶ Ignore messages from other chats
   │
5. ├─▶ For each new message:
   │    └─▶ Save to DB (Status = New)
   │
6. ├─▶ For each unprocessed message (Status != Done):
   │    │
   │    ├─▶ Update Status = InProgress
   │    │
   │    ├─▶ Download media to MediaPath
   │    │    └─▶ Filename: {datetime}_{chatId}_{messageId}
   │    │
   │    ├─▶ Transcribe audio using Whisper.net
   │    │
   │    ├─▶ Create Obsidian note
   │    │    ├─▶ Add YAML frontmatter (created, id, url)
   │    │    └─▶ Save to ObsidianPath/inbox
   │    │
   │    ├─▶ Update DB (Status = Done, DocumentPath = note_path)
   │    │
   │    └─▶ Reply to Telegram message with link + GUID
   │
7. └─▶ END
```

---

## Configuration Example

### appsettings.json

```json
{
  "AppSettings": {
    "BotKey": "{NAME_ENV_VARIABLE_TO_GET_KEY}",
    "ChatIds": [1123123, 1313132, 42423],
    "ObsidianPath": "/home/user/Documents/ObsidianVault",
    "MediaPath": "/home/user/Documents/ObsidianVault/media",
    "DailyNotePath": "{ObsidianPath}/{yyyy}/{MM}/{yyyy-MM-dd}.md",
    "WhisperModelPath": "./models/ggml-base.bin"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=noterouter.db"
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/log-.txt", "rollingInterval": "Day" } }
    ]
  }
}
```

### Environment Variables

```bash
export APPSETTINGS__BOTKEY="your-telegram-bot-token"
# Or use user-secrets for development
dotnet user-secrets set "AppSettings:BotKey" "your-telegram-bot-token"
```

---

## Whisper Model Setup

Download a Whisper model (ggml format) for Whisper.net:

```bash
# Base model (~150MB) - good balance of speed/accuracy
wget https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin

# Small model (~500MB) - better accuracy
wget https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin

# Tiny model (~75MB) - fastest, lower accuracy
wget https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin
```

---

## Notes

- Bot must be added to the Telegram groups it needs to monitor
- Only messages from whitelisted `ChatIds` will be processed
- Whisper.net runs locally - no external API required
- Consider using `IHostedService` for background processing
- EF Core migrations run automatically on startup
- Implement proper error handling for network failures
- Consider rate limiting for Telegram API calls
