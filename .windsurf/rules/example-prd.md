# User management PRD

A brief high-level introduction: The User management feature enables tenants to create, manage, and delete users within their tenant, including role assignment and permission management.

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

## Core features
- **User creation**
  - Creation of users by Tenant Owners or Admins only.
  - Email must be unique within the tenant.
  - Role assignment during creation (Owner, Admin, Member).
  - Email invitation sent to new users.

- **User management**
  - Update user full name and role by Tenant Owners or Admins.
  - Activate/deactivate user accounts.
  - Delete users (soft delete with option to restore).
  - Cannot delete Tenant Owner (last owner cannot be removed).

- **Visibility**
  - All users can see the list of users within their tenant.
  - Only Tenant Owners can see sensitive information (email history, activity).
  - Users can manage their own profile.

## User experience
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

## Slices overview
This is a very high-level overview of the slices needed to implement the feature.

### 1. Backend for user management
This slice implements the core user functionality including user aggregate, domain model, database migration, CRUD operations, API endpoints, and tests. It enables creating, updating, deactivating, and deleting users with proper validation and permission checks.

### 2. Frontend for user management
This slice implements the Users page UI with the ability to list, create, edit, and deactivate users using the API from the first slice. It includes the navigation menu item and user management interface with role assignment.

### 3. User invitation and email notifications
This slice implements the email invitation system for new users and password reset functionality. It includes email templates, invitation token generation, and notification delivery.

### 4. End-to-end tests
This slice implements comprehensive Playwright tests covering user creation, editing, deletion, role changes, and permission validation across different user roles.