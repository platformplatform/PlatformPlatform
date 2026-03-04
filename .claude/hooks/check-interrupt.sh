#!/bin/bash

# PostToolUse hook: checks for interrupt signals from teammates.
#
# To send an interrupt to an agent:
#   echo "STOP: reason" > ~/.claude/teams/<team>/signals/<agent-name>.signal
#
# How it works:
# - Runs after every tool call (no matcher = all tools)
# - Scans ALL teams' signals/ directories for any file
# - Deduplicates by agent name (same agent in multiple teams = one report)
# - Outputs a short interrupt line per agent
# - Does NOT clear signal files -- the target agent deletes its own file(s)
# - Agents self-select: only the named agent acts, others ignore

# Read JSON input from stdin (required by hook protocol)
cat > /dev/null

CONTEXT=""
SEEN_AGENTS=""

for SIGNALS_DIR in "$HOME/.claude/teams"/*/signals; do
  [ -d "$SIGNALS_DIR" ] || continue

  for SIGNAL_FILE in "${SIGNALS_DIR}"/*; do
    [ -f "$SIGNAL_FILE" ] || continue
    [ -s "$SIGNAL_FILE" ] || continue

    FILENAME=$(basename "$SIGNAL_FILE")
    AGENT_NAME="${FILENAME%.*}"

    # Deduplicate: skip if we already reported this agent
    case ",$SEEN_AGENTS," in
      *,"$AGENT_NAME",*) continue ;;
    esac
    SEEN_AGENTS="${SEEN_AGENTS:+$SEEN_AGENTS,}$AGENT_NAME"

    MESSAGE=$(cat "$SIGNAL_FILE")
    ESCAPED_MSG=$(printf '%s' "$MESSAGE" | sed 's/\\/\\\\/g; s/"/\\"/g' | tr '\n' ' ')

    # Collect all signal files for this agent across teams (for cleanup)
    RM_CMDS=""
    for D in "$HOME/.claude/teams"/*/signals; do
      F="${D}/${FILENAME}"
      [ -f "$F" ] && RM_CMDS="${RM_CMDS} ${F}"
    done

    CONTEXT="${CONTEXT}INTERRUPT [${AGENT_NAME}]: ${ESCAPED_MSG} (if you are [${AGENT_NAME}], act on it then run: rm${RM_CMDS}) /// "
  done
done

if [ -n "$CONTEXT" ]; then
  cat <<ENDJSON
{
  "decision": "block",
  "reason": "${CONTEXT}"
}
ENDJSON
fi

exit 0
