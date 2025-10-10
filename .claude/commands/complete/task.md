---
description: Signal task completion and terminate worker session
---

# Complete Task

**For Engineers Only**: backend-engineer, frontend-engineer, test-automation-engineer

Call this when you've finished your task or are stuck.

## Steps

1. Write comprehensive response (what you accomplished, notes for Tech Lead)
2. Create brief summary (sentence case, e.g., "Api endpoints implemented")
3. **Mark the "Call /complete/task to signal completion" todo as completed** using TodoWrite
4. Call MCP **CompleteTask** tool:
   - `agentType`: Your agent type
   - `taskSummary`: Your summary
   - `responseContent`: Your full response

Your session terminates immediately after step 4.
