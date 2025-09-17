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

Task processing template - YOU MUST follow this exact format:
- CLI returns: `task_0001_fix_warnings.md`
- Read: `cat task_0001_fix_warnings.md` (see full context)
- Process the task
- Increment task number and append your structured response:
  ```bash
  # Clear your todo list first
  TodoWrite '[]'

  # Append structured response
  echo "

**$(basename \"$PWD\")** - $(date)

## Summary
[Concise summary of what you accomplished]

## Problems
[Any issues found or remaining problems]

## Next Action
[What should happen next - which agent should work on this]

" >> task_0001_fix_warnings.md

  # Update task number and move to next agent or back to coordinator
  NEW_TASK_ID=$(printf "%04d" $(($(echo "task_0001_fix_warnings.md" | grep -o '[0-9]*') + 1)))
  mv task_0001_fix_warnings.md "../coordinator/message-queue/task_${NEW_TASK_ID}_fix_warnings.md"
  ```
- Restart: `/monitor`

CRITICAL: Always clear todos, use structured template, increment number, specify next action.

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