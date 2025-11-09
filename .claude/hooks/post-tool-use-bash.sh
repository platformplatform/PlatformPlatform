#!/bin/bash

# Post-tool-use hook for Bash commands
# Reminds AI about git commit restrictions after git operations

# Read the JSON input from stdin
input=$(cat)

# Extract the command from the JSON input
cmd=$(echo "$input" | sed -n 's/.*"command":"\([^"]*\)".*/\1/p')

# If it was a git commit, remind about commit restrictions
case "$cmd" in
    *"git commit"*) echo "⚠️ CRITICAL: NEVER proactively commit or suggest committing. Changes MUST always be reviewed BEFORE committing. Only commit when explicitly instructed to." >&2 ;;
esac

exit 0  # Always allow post-tool execution to proceed
