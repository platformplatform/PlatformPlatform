# User Management PRD

A brief high-level introduction: The User Management feature enables tenants to create, manage, and delete users within their tenant, including role assignment and permission management.

## Overview
- Users exist within a single tenant and have unique email addresses across that tenant.
- **UserId**: strongly-typed ID with `user` prefix to ensure type safety.
- **Email**: unique per tenant, valid email format.
- **Full Name**: user's display name (≤ 100 characters).
- **Role**: assigned role determining permissions (Owner, Admin, Member).
- **Status**: Active or Inactive.
- **TenantId**: strongly-typed ID linking the user to its tenant.
- Tenant Owners have full control over all users and their configuration.
- Users can only be managed by Tenant Owners or Admins.

## Core Features
- **User creation:**
  - Creation of users by Tenant Owners or Admins only.
  - Email must be unique within the tenant.
  - Role assignment during creation (Owner, Admin, Member).
  - Email invitation sent to new users.

- **User management:**
  - Update user full name and role by Tenant Owners or Admins.
  - Activate/deactivate user accounts.
  - Delete users (soft delete with option to restore).
  - Cannot delete Tenant Owner (last owner cannot be removed).

- **Visibility:**
  - All users can see the list of users within their tenant.
  - Only Tenant Owners can see sensitive information (email history, activity).
  - Users can manage their own profile.

## User Experience
- New "Users" menu option in the Tenant settings section of the shared navigation.
- Users page at `/admin/users` displays a list of all users with ability to add new users.
- Selecting a user opens a side panel showing user details (email, name, role, status).
- User management options are conditionally displayed based on role:
  - Tenant Owners see all management options.
  - Admins see member management options but not owner-level settings.
  - Members see only their own profile.
- Simple forms for user creation and editing with validation for email uniqueness.
- Confirmation dialogs for sensitive actions like deactivating or deleting users.
- All operations strictly scoped to the current tenant to maintain isolation.

## Tasks Overview
This is a very high-level overview of the tasks needed to implement the feature.

### 1. Backend for User Management

This task implements the core user functionality including user aggregate, domain model, database migration, CRUD operations, API endpoints, and tests. It enables creating, updating, deactivating, and deleting users with proper validation and permission checks.

- Create `UserId` strongly typed ID with `user` prefix.
- Create `User` aggregate with email, full name, role, status, and tenant ID.
- Create `IUserRepository` interface and implementation.
- Create `UserConfiguration` for EF Core mapping.
- Create database migration for Users table.
- Create `CreateUser` command and handler with validation.
- Create `UpdateUser` command and handler.
- Create `DeleteUser` command (soft delete) and handler.
- Create `GetUser` query and handler.
- Create `GetUsers` query and handler with filtering.
- Create API endpoints for all commands and queries.
- Create comprehensive API tests for all operations.

### 2. Frontend for User Management

This task implements the Users page UI with the ability to list, create, edit, and deactivate users using the API from the first task. It includes the navigation menu item and user management interface with role assignment.

- Add Users navigation menu item in Organization section.
- Create Users page route at `/admin/users`.
- Create UsersPage component structure.
- Create UsersTable component with API integration.
- Create CreateUserDialog component.
- Create UserDetailsSidePane component.
- Create EditUserDialog component.
- Integrate all API endpoints using TanStack Query.
- Add loading states and error handling.
- Implement permission-based UI controls.

### 3. User Invitation and Email Notifications

This task implements the email invitation system for new users and password reset functionality. It includes email templates, invitation token generation, and notification delivery.

- Create invitation token generation logic.
- Create email templates for user invitations.
- Create SendUserInvitation command and handler.
- Create email delivery integration.
- Create API endpoint for sending invitations.
- Create API tests for invitation flow.

### 4. End-to-End Tests for User Management

This task implements a single @smoke Playwright test that covers the entire user management flow. We consolidate into one test to minimize browser startup overhead and test execution time - many small tests are slow due to repeated setup/teardown.

- Create test fixtures for user management scenarios.
- Create ONE @smoke test that covers:
  - User creation flow.
  - User editing.
  - User deletion.
  - Role-based permissions.
