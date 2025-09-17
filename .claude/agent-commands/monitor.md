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
2. If filename contains "thread_": Read, append your findings, move back to sender
3. If filename contains "response_": Read, process information, delete file
4. Run /monitor again to continue monitoring

Example thread processing:
- CLI returns: `thread_0001_fix_warnings.md`
- Read: `cat thread_0001_fix_warnings.md`
- Process the original request
- Append your findings:
  ```bash
  echo "

**$(basename "$PWD")** - $(date)
Task completed: [your detailed findings and results]
Fixed 3 warnings in UserService.cs and ApiController.cs
All tests now passing

---
Status: Completed by $(basename "$PWD")" >> thread_0001_fix_warnings.md

# IMPORTANT: Clear your todo list before sending thread back
TodoWrite '[]'
  ```
- Move back: `mv thread_0001_fix_warnings.md ../coordinator/message-queue/`
- Restart: `/monitor`

The thread file grows with each agent's contributions, providing full context.

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