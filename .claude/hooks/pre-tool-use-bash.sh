#!/bin/bash

# Pre-tool-use hook for Bash commands
# This hook blocks certain commands and enforces MCP tool usage

# Read the JSON input from stdin
input=$(cat)

# Extract the command from the JSON input
cmd=$(echo "$input" | sed -n 's/.*"command":"\([^"]*\)".*/\1/p')

# Check the command and decide whether to block it
case "$cmd" in
    *"git merge "*|*"git rebase "*|*"git reset "*|*"git revert "*|*"git tag "*|*"git clean "*|*"git push "*|*"git push"*|*"git remote "*|*"git config "*) echo "❌ Dangerous git operation. Run this yourself." >&2; exit 2 ;;
    *"dotnet build"*) echo "❌ Use **build MCP tool** instead" >&2; exit 2 ;;
    *"dotnet test"*) echo "❌ Use **test MCP tool** instead" >&2; exit 2 ;;
    *"dotnet format"*) echo "❌ Use **format MCP tool** instead" >&2; exit 2 ;;
    *"npm run format"*) echo "❌ Use **format MCP tool** instead" >&2; exit 2 ;;
    *"npm test"*) echo "❌ Use **test MCP tool** instead" >&2; exit 2 ;;
    *"npm run build"*) echo "❌ Use **build MCP tool** instead" >&2; exit 2 ;;
    *"npx playwright test"*) echo "❌ Use **end-to-end MCP tool** instead" >&2; exit 2 ;;
    *"docker"*) echo "❌ Docker not allowed. Use **watch MCP tool** for Aspire/migrations" >&2; exit 2 ;;
    *) exit 0 ;;
esac
