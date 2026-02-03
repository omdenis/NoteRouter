# NoteRouter Implementation Plan

## Project Overview

NoteRouter is a Python Telegram bot that automatically processes messages from specified Telegram groups, downloads media files, transcribes audio to text, and saves the results as Obsidian notes.

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
                        └──────────────┘
```

---

## Components

### 1. Configuration Module
- Load configuration from `.env` or config file
- Environment variables:
  - `BOT_KEY` - Telegram bot token (from environment variable)
  - `CHAT_IDS` - Comma-separated list of allowed chat IDs
  - `OBSIDIAN_PATH` - Path to Obsidian vault
  - `MEDIA_PATH` - Path to store downloaded media
  - `DAILY_NOTE` - Template path for daily notes (e.g., `/path/{YYYY}/{MM}/{YYYY-MM-DD}.md`)

### 2. Database Module
- SQLite local database
- Table: `messages`
  - `id` - Primary key
  - `message_id` - Telegram message ID
  - `chat_id` - Telegram chat ID
  - `message_path` - Path to the original message
  - `status` - Processing status (`new`, `in_progress`, `done`)
  - `document_path` - Path to created Obsidian note
  - `guid` - Unique identifier for the note
  - `created_at` - Timestamp

### 3. Telegram Module
- Connect to Telegram API using bot token
- Fetch new messages from allowed chats only
- Download media files (audio, voice, video, documents)
- Send reply messages with processing results

### 4. Media Processor
- Download files to `MEDIA_PATH`
- Filename format: `{datetime}_{chat_id}_{message_id}.{ext}`
- Support for various media types

### 5. Transcription Module
- Audio-to-text transcription
- Options: OpenAI Whisper (local or API), or other STT service

### 6. Obsidian Note Generator
- Create markdown notes in `OBSIDIAN_PATH/inbox`
- Filename format: `{date}_{time}_{chat_id}.md`
- Frontmatter properties:
  ```yaml
  ---
  created: YYYY-MM-DD
  id: {GUID}
  url: {telegram_message_url}
  ---
  ```

### 7. Scheduler/Runner
- Designed to run once per day (via cron or scheduler)
- Process flow:
  1. Fetch new messages
  2. Save to database with `status = new`
  3. Process each message sequentially
  4. Update status and send replies

---

## Implementation Tasks

### Phase 1: Project Setup
- [ ] Initialize Python project structure
- [ ] Set up virtual environment
- [ ] Create `pyproject.toml` or `requirements.txt`
- [ ] Set up configuration loading (python-dotenv)

### Phase 2: Database
- [ ] Create SQLite database schema
- [ ] Implement database connection/session management
- [ ] Implement CRUD operations for messages table
- [ ] Add migration support (optional)

### Phase 3: Telegram Integration
- [ ] Set up Telegram bot client (python-telegram-bot or Telethon)
- [ ] Implement message fetching from specific chats
- [ ] Implement chat ID validation (whitelist only)
- [ ] Implement media file downloading
- [ ] Implement reply message sending

### Phase 4: Media Processing
- [ ] Implement file download to MEDIA_PATH
- [ ] Generate unique filenames
- [ ] Handle different media types (audio, voice, video, images, documents)

### Phase 5: Transcription
- [ ] Integrate speech-to-text service
- [ ] Handle audio/voice message transcription
- [ ] Error handling for failed transcriptions

### Phase 6: Obsidian Integration
- [ ] Create note generator with frontmatter
- [ ] Generate GUIDs for notes
- [ ] Create Obsidian URI links
- [ ] Save notes to inbox folder

### Phase 7: Main Processing Pipeline
- [ ] Implement main processing loop
- [ ] Status management (new → in_progress → done)
- [ ] Error handling and retry logic
- [ ] Telegram reply with Obsidian link and GUID

### Phase 8: Deployment
- [ ] Create entry point script
- [ ] Document cron job setup for daily execution
- [ ] Add logging
- [ ] Create example configuration file

---

## Project Structure

```
NoteRouter/
├── src/
│   └── noterouter/
│       ├── __init__.py
│       ├── main.py              # Entry point
│       ├── config.py            # Configuration loading
│       ├── database/
│       │   ├── __init__.py
│       │   ├── models.py        # SQLAlchemy models
│       │   └── repository.py    # Database operations
│       ├── telegram/
│       │   ├── __init__.py
│       │   ├── client.py        # Telegram bot client
│       │   └── downloader.py    # Media downloader
│       ├── processing/
│       │   ├── __init__.py
│       │   ├── transcriber.py   # Audio transcription
│       │   └── pipeline.py      # Processing pipeline
│       └── obsidian/
│           ├── __init__.py
│           └── note_generator.py # Note creation
├── tests/
├── docs/
├── .env.example
├── pyproject.toml
└── README.md
```

---

## Dependencies

- `python-telegram-bot` or `telethon` - Telegram API
- `python-dotenv` - Environment configuration
- `sqlalchemy` - Database ORM
- `openai-whisper` or `openai` - Audio transcription
- `pydantic` - Data validation
- `uuid` - GUID generation

---

## Processing Flow

```
1. START (scheduled daily)
   │
2. ├─▶ Connect to Telegram
   │
3. ├─▶ Fetch messages from allowed CHAT_IDS
   │    └─▶ Ignore messages from other chats
   │
4. ├─▶ For each new message:
   │    └─▶ Save to DB (status = new)
   │
5. ├─▶ For each unprocessed message (status != done):
   │    │
   │    ├─▶ Update status = in_progress
   │    │
   │    ├─▶ Download media to MEDIA_PATH
   │    │    └─▶ Filename: {datetime}_{chat_id}_{message_id}
   │    │
   │    ├─▶ Transcribe audio (if applicable)
   │    │
   │    ├─▶ Create Obsidian note
   │    │    ├─▶ Add frontmatter (created, id, url)
   │    │    └─▶ Save to OBSIDIAN_PATH/inbox
   │    │
   │    ├─▶ Update DB (status = done, path = note_path)
   │    │
   │    └─▶ Reply to Telegram message with link + GUID
   │
6. └─▶ END
```

---

## Configuration Example

```ini
# .env
BOT_KEY=${TELEGRAM_BOT_TOKEN}
CHAT_IDS=1123123,1313132,42423
OBSIDIAN_PATH=/home/user/Documents/ObsidianVault
MEDIA_PATH=/home/user/Documents/ObsidianVault/media
DAILY_NOTE=/home/user/Documents/ObsidianVault/{YYYY}/{MM}/{YYYY-MM-DD}.md
```

---

## Notes

- Bot must be added to the Telegram groups it needs to monitor
- Only messages from whitelisted `CHAT_IDS` will be processed
- Transcription service may require additional API keys (e.g., OpenAI)
- Consider rate limiting for Telegram API calls
- Implement proper error handling for network failures
