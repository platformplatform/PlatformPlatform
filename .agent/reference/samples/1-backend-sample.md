# Backend for User Management

**This is an example of how to describe a backend [task] in the PRD.**

This task implements the core user functionality including user aggregate, domain model, database migration, CRUD operations, API endpoints, and tests. It enables creating, updating, deactivating, and deleting users with proper validation and permission checks.

## Subtasks (Implementation Guidance)

The following subtasks guide the engineer through implementing this complete vertical slice. These are bullets in the task description (not separately tracked items):

- Create `UserId` strongly typed ID with `user` prefix
- Create `User` aggregate with email, full name, role, status, and tenant ID
- Create `IUserRepository` interface and implementation
- Create `UserConfiguration` for EF Core mapping
- Create database migration for Users table
- Create `CreateUser` command and handler with email uniqueness validation
- Create `UpdateUser` command and handler with permission checks
- Create `DeleteUser` command (soft delete) and handler
- Create `GetUser` query and handler
- Create `GetUsers` query and handler with filtering by name and email
- Create API endpoints for all commands and queries
- Create comprehensive API tests for all operations including edge cases

## Important Notes

**This entire task = ONE commit:**
- All subtasks implemented together
- Code compiles, runs, and passes tests after completion
- Engineer builds and tests incrementally after each subtask
- Final validation before review includes format and inspect

**NOT included in this task:**
- Frontend implementation (separate task)
- User role management (separate task if needed)
- Email invitation functionality (separate task)
