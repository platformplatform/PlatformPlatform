---
description: Start 2-hour FOREGROUND message monitoring - MUST NOT run in background
---

# Message Monitoring with 2-Hour Timeout

You MUST run this command in FOREGROUND with a 2-hour timeout. This normally executes quickly but the 2-hour timeout allows for continuous monitoring.

CRITICAL: This MUST run in FOREGROUND mode. Do NOT run as background task.

```bash
echo "🤖 Monitoring: $PWD/message-queue"

pp claude-agent-process-message-queue "$PWD"

echo "🔄 Check complete - run /monitor again to continue"
```

When the CLI returns file paths, you MUST process ALL files and DELETE them:
1. Read EVERY file with `cat [filepath]`
2. Process all tasks/responses as this agent
3. For requests: Create responses and move to sender
4. For responses: Process the information
5. DELETE ALL processed files with `rm [filepath]`
6. Run /monitor again to continue monitoring

CRITICAL: You MUST delete ALL files after processing or you'll keep seeing the same files repeatedly.

Keep-alive messages (keepalive_*.md) should be silently deleted - just run `rm` on them without processing.

Example:
- CLI returns: `message-queue/request_0001_from_coordinator.md`
- Read: `cat message-queue/request_0001_from_coordinator.md`
- Process the task
- Modify the SAME file with response:
  ```bash
  cat > message-queue/request_0001_from_coordinator.md <<EOF
  ---
  # Response 0001 from backend - $(date)
  Your task results here

  Status: Completed
  ---
  EOF
  ```
- Rename and move: `mv message-queue/request_0001_from_coordinator.md ../coordinator/message-queue/response_0001_from_backend.md`
- Restart: `/monitor`