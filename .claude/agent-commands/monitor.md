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

When the CLI returns file paths, you MUST handle ALL files:

For EACH file path:
1. If filename contains "keepalive_": DELETE immediately with `rm [filepath]` (no processing needed)
2. If filename contains "request_": Read, process task, create response, move to sender, delete original
3. If filename contains "response_": Read, process information, delete file
4. Run /monitor again to continue monitoring

CRITICAL: ALWAYS delete ALL files or you'll see them repeatedly.

Example:
- `keepalive_20250917_015305.md` → `rm keepalive_20250917_015305.md` (silent deletion)
- `request_0001_from_coordinator.md` → Read, process, respond, delete

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