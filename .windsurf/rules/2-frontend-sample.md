# Frontend for user management

**Purpose:**

This slice implements the Users page UI with the ability to:
- List, create, edit, and delete users using the API from the first slice
- Display users in a table with user data
- Create new users via modal dialog
- View and edit user details in a side pane
- Implement appropriate loading and empty states
- Apply permission-based UI controls
- Add Users menu item to the Organization section of navigation

**NOT included:**
- Backend implementation (previous slice)
- User role management (separate slice)
- Advanced filtering/sorting
- Pagination and search functionality

**Dependencies:**
- Backend for user management slice must be completed first

**IMPORTANT:**
Do not modify any backend code or implement user role functionality in this slice.

## CRITICAL: Frontend vertical slice requirements

**EACH TASK = ONE COMMIT = ONE COMPLETE UI FEATURE**

A vertical slice in frontend means:
- Component + API integration + UI state + user interaction = ONE task/commit
- The UI must be functional and testable after EACH task
- Never split component creation, API calls, and state management into separate tasks

**Example of WRONG approach (multiple commits):**
- Task 1: Create table component structure ❌
- Task 2: Add API data fetching ❌
- Task 3: Connect data to table ❌
- Task 4: Add loading states ❌
- Task 5: Add error handling ❌

**Example of RIGHT approach (vertical slice):**
- Task 1: Create users table with data fetching and loading states ✅
  (Complete working table in ONE commit)