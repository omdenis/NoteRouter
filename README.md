# NoteRouter

A Telegram bot that automatically captures voice messages and media from specified Telegram groups, transcribes audio using Whisper, and saves everything as organized notes in your Obsidian vault.

## How It Works

1. **Listen** - Bot monitors configured Telegram groups for new messages
2. **Download** - Media files (voice, audio, video) are saved locally
3. **Transcribe** - Audio content is converted to text using Whisper AI
4. **Save** - Notes are created in Obsidian with proper metadata (date, source link, unique ID)
5. **Notify** - Bot replies to the original message with a link to the created note

## Use Case

You have a Telegram group where you and your team share voice notes, ideas, and discussions. NoteRouter runs daily, processes all new messages, transcribes voice content, and saves everything to your Obsidian vault's inbox folder - making all your Telegram content searchable and organized.

## Configuration

```json
{
  "AppSettings": {
    "BotKey": "TELEGRAM_BOT_TOKEN",
    "ChatIds": [123456789, 987654321],
    "ObsidianPath": "/path/to/obsidian/vault",
    "MediaPath": "/path/to/obsidian/vault/media",
    "WhisperModelPath": "./models/ggml-base.bin"
  }
}
```

## Requirements

- .NET 10
- Telegram Bot Token
- Whisper model file (ggml format)

## Quick Start

```bash
# Set your bot token
export TELEGRAM_BOT_TOKEN="your-token-here"

# Run
dotnet run --project src/NoteRouter
```

## Generated Notes

Each processed message creates a markdown note with frontmatter:

```markdown
---
created: 2024-01-15
id: a1b2c3d4-e5f6-7890-abcd-ef1234567890
url: https://t.me/c/123456789/42
---

[Transcribed content here]
```
