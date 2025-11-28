# Frontend for User Management

**This is an example of how to describe a frontend [task] in the PRD.**

This task implements the Users page UI with the ability to list, create, edit, and deactivate users using the API from the backend task. It includes the navigation menu item, user management interface with role assignment, and all necessary UI components.

## Subtasks (Implementation Guidance)

The following subtasks guide the engineer through implementing this complete UI feature. These are bullets in the task description (not separately tracked items):

- Add Users navigation menu item in Organization section.
- Create Users page route at `/admin/users`.
- Create UsersPage component with layout structure.
- Create UsersTable component with columns for email, name, role, status.
- Integrate `GetUsers` query using TanStack Query.
- Create CreateUserDialog component with form validation.
- Integrate `CreateUser` command with optimistic updates.
- Create UserDetailsSidePane component.
- Integrate `GetUser` query for side pane data.
- Create EditUserDialog component.
- Integrate `UpdateUser` command.
- Integrate `DeleteUser` command with confirmation dialog.
- Add loading states for all async operations.
- Add error handling and toast notifications.
- Implement permission-based UI (show/hide actions based on role).
- Test all functionality in Chrome DevTools (network, console).
- Verify translations in all supported languages.

## Important Notes

**This entire task = ONE commit:**
- All subtasks implemented together.
- UI is functional and visually complete after implementation.
- Engineer builds incrementally after each subtask.
- Final validation includes build, format, inspect, and Chrome DevTools testing.

**NOT included in this task:**
- Backend API implementation (separate task).
- Advanced filtering or search (separate task if needed).
- Pagination (separate task if needed).
