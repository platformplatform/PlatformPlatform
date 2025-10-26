# Backend for user management

**Purpose:**

This slice implements the core user functionality including:
- User aggregate and domain model
- Database migration for the Users table
- CRUD operations for users
- API endpoints for user management
- API tests for all operations with proper validation and permission checks

**NOT included:**
- Frontend implementation (next slice)
- User role management (separate slice)
- User interface components
- Authentication/authorization UI

**Dependencies:**
- None (this is the first slice)

**IMPORTANT:**
Do not implement frontend components or user role functionality in this slice.

## CRITICAL: Vertical slice requirements

**EACH TASK = ONE COMMIT = ONE VERTICAL SLICE**

A vertical slice means:
- Database changes + backend logic + API endpoint + tests = ONE task/commit
- The code must compile, run, and be testable after EACH task
- Never split database, backend, API, and tests into separate tasks

**Example of WRONG approach (multiple commits):**
- Task 1: Create database migration ❌
- Task 2: Create User aggregate ❌
- Task 3: Create CreateUser command ❌
- Task 4: Create API endpoint ❌
- Task 5: Create tests ❌

**Example of RIGHT approach (vertical slice):**
- Task 1: Create user aggregate, command, endpoint, migration and tests ✅
  (Everything needed to create a user in ONE commit)

