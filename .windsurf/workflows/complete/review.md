---
description: Workflow for signal review completion and terminate reviewer session
auto_execution_mode: 1
---

# Complete Review

**For Reviewers Only**: backend-reviewer, frontend-reviewer, test-automation-reviewer

Call this when you've finished reviewing.

## Steps

1. Make decision: Approved or Rejected?
2. Write comprehensive review feedback
3. Create brief summary (sentence case, e.g., "Excellent implementation" or "Missing test coverage")
4. Call MCP **CompleteReview** tool:
   - `agentType`: Your agent type
   - `approved`: true or false
   - `reviewSummary`: Your summary
   - `responseContent`: Your full review

Your session terminates immediately.

---

## Examples

**✅ DO: Use sentence case summaries**
```
approved: true
reviewSummary: "Excellent implementation"
```
```
approved: false
reviewSummary: "Missing test coverage"
```

**❌ DON'T: Use generic or vague summaries**
```
approved: true
reviewSummary: "Good"
```
```
approved: false
reviewSummary: "Issues found"
```

**✅ DO: Be specific about what's good or missing**
```
"Clean architecture and comprehensive tests"
"Missing validation for edge cases"
"Incorrect use of strongly typed IDs"
```

**❌ DON'T: Use technical jargon without context**
```
"LGTM"
"Needs refactoring"
"Bad code"
```