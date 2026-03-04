#!/bin/bash

# Helper script to send an interrupt signal to a team agent.
#
# Usage:
#   send-interrupt.sh <team-name> <agent-name> <message>
#
# Examples:
#   send-interrupt.sh feature-team backend "STOP: Wrong file, use Api/Program.cs"
#   send-interrupt.sh feature-team frontend "API contract changed, re-read spec"

TEAM_NAME="$1"
AGENT_NAME="$2"
MESSAGE="$3"

if [ -z "$TEAM_NAME" ] || [ -z "$AGENT_NAME" ] || [ -z "$MESSAGE" ]; then
  echo "Usage: send-interrupt.sh <team-name> <agent-name> <message>" >&2
  exit 1
fi

SIGNALS_DIR="$HOME/.claude/teams/${TEAM_NAME}/signals"

if [ ! -d "$SIGNALS_DIR" ]; then
  mkdir -p "$SIGNALS_DIR"
fi

SIGNAL_FILE="${SIGNALS_DIR}/${AGENT_NAME}.signal"
echo "$MESSAGE" > "$SIGNAL_FILE"
echo "Interrupt sent to ${AGENT_NAME} in ${TEAM_NAME}"
