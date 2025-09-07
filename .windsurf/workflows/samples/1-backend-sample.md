---
---

# Backend for team management

**Purpose:**

This increment implements the core team functionality including:
- Team aggregate and domain model
- Database migration for the Teams table
- CRUD operations for teams
- API endpoints for team management
- API tests for all operations with proper validation and permission checks

**NOT included:**
- Frontend implementation (next increment)
- Team member management (separate increment)
- User interface components
- Authentication/authorization UI

**Dependencies:**
- None (this is the first increment)

**IMPORTANT:**
Do not implement frontend components or team membership functionality in this increment.

## CRITICAL: Vertical slice requirements

**EACH TASK = ONE COMMIT = ONE VERTICAL SLICE**

A vertical slice means:
- Database changes + backend logic + API endpoint + tests = ONE task/commit
- The code must compile, run, and be testable after EACH task
- Never split database, backend, API, and tests into separate tasks

**Example of WRONG approach (multiple commits):**
- Task 1: Create database migration ❌
- Task 2: Create Team aggregate ❌  
- Task 3: Create CreateTeam command ❌
- Task 4: Create API endpoint ❌
- Task 5: Create tests ❌

**Example of RIGHT approach (vertical slice):**
- Task 1: Create team aggregate, command, endpoint, migration and tests ✅
  (Everything needed to create a team in ONE commit)

