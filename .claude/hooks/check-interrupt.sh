#!/bin/bash

# PostToolUse hook: checks for interrupt signals targeted at THIS agent.
# Uses atomic rename (mv) as the sole gate -- no file existence check that can race.

INPUT=$(cat)
AGENT_NAME=$(printf '%s' "$INPUT" | sed -n 's/.*"agent_type":"\([^"]*\)".*/\1/p')
[ -z "$AGENT_NAME" ] && exit 0

for SIGNALS_DIR in "$HOME/.claude/teams"/*/signals; do
  [ -d "$SIGNALS_DIR" ] || continue
  SIGNAL_FILE="${SIGNALS_DIR}/${AGENT_NAME}.signal"
  TMP_FILE="${SIGNAL_FILE}.claimed.$$"
  mv "$SIGNAL_FILE" "$TMP_FILE" 2>/dev/null || continue
  MESSAGE=$(cat "$TMP_FILE")
  rm -f "$TMP_FILE"
  [ -z "$MESSAGE" ] && continue
  ESCAPED_MSG=$(printf '%s' "$MESSAGE" | sed 's/\\/\\\\/g; s/"/\\"/g' | tr '\n' ' ')
  printf '{"decision":"block","reason":"INTERRUPT: %s"}\n' "$ESCAPED_MSG"
  exit 0
done
exit 0
