---
description: Send message and wait for replies - requires 2-hour timeout
argument-hint: [recipient-agent] [task-description]
---

Send message to: $1
Message: $2

CRITICAL: This command MUST run in FOREGROUND with a 2-hour timeout. This is normally very fast but we allow 2 hours for the reply monitoring.

DO NOT START AS BACKGROUND TASK. MUST run in foreground mode and block the terminal for up to 2 hours.

```bash
SENDER=$(basename "$PWD"); RECIPIENT="$1"; MESSAGE="$2"; FEATURE=$(basename "$(dirname "$PWD")")

# Generate stable 4-digit ID
COUNTER_FILE=".command_counter"
if [ -f "$COUNTER_FILE" ]; then
    COUNTER=$(cat "$COUNTER_FILE")
else
    COUNTER=0
fi
COUNTER=$((COUNTER + 1))
echo $COUNTER > "$COUNTER_FILE"
MSG_ID=$(printf "%04d" $COUNTER)

# Create message in recipient's queue with ID
RECIPIENT_DIR="$(git rev-parse --show-toplevel)/.claude/agent-workspaces/$FEATURE/$RECIPIENT"
mkdir -p "$RECIPIENT_DIR/message-queue"

cat > "$RECIPIENT_DIR/message-queue/thread_${MSG_ID}_$(echo "$MESSAGE" | tr ' ' '_' | cut -c1-20).md" <<EOF
---
# Thread $MSG_ID: $MESSAGE
Started by: $SENDER
Created: $(date)

## Original Request
$MESSAGE

## Thread History

**$SENDER** - $(date)
Initial request: $MESSAGE

---
Status: Pending - Assigned to $RECIPIENT
EOF

echo "✅ Message $MSG_ID sent to $RECIPIENT"
echo -e "\033[31m🎯 ACTIVE AGENT: $RECIPIENT is now working on this task\033[0m"
echo "⏳ Waiting for reply $MSG_ID..."
```

After sending the message, you MUST run this command with 2-hour timeout:

`pp claude-agent-process-message-queue "$PWD"`

When the CLI outputs file paths, you MUST process EVERY SINGLE FILE PATH:

For EACH file path the CLI outputs:
1. Read the file: `cat [exact-filepath]`
2. Process the reply content

After reading ALL files:
3. Delete ALL files: `rm [filepath1] [filepath2] [filepath3]` etc.

YOU MUST READ EVERY FILE PATH RETURNED - NOT JUST ONE.

Example: CLI outputs 6 paths → Read all 6 files → Process all 6 replies → Delete all 6 files.