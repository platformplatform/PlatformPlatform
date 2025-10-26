# Users management PRD

A brief high-level introduction: The Users management feature enables tenants to organize users into named groups with clear membership roles and permissions, allowing for better collaboration and access control within the organization.

## Overview
- Users exist within a single tenant and have unique names across that tenant.
- **UserId**: strongly-typed ID with `user` prefix to ensure type safety.
- **Name**: unique per tenant, ≤ 50 Unicode characters to prevent naming conflicts.
- **Description**: ≤ 255 Unicode characters (allow empty string).
- **TenantId**: strongly-typed ID linking the user to its tenant.
- Users can belong to multiple users within a tenant.
- User membership has two roles: regular **Member** and **Admin**.
- User Admins can manage user membership but cannot demote themselves.
- Tenant Owners have full control over all users and their configuration.
- All users on a tenant can see all users, but only see user members of users they belong to.

## Core features
- **User management**  
  - Creation and deletion of users by Tenant Owners only.
  - User name and description can only be modified by Tenant Owners.
  - Users must have unique names within a tenant.
- **Membership management**  
  - Addition/removal of members by User Admins or Tenant Owners.
  - Promotion of members to Admin role by User Admins or Tenant Owners.
  - User Admins cannot demote themselves from the Admin role.
  - Users can be members of any number of users.
- **Visibility**  
  - All users can see the list of all users within their tenant.
  - Only user members can see the member roster of users they belong to.
  - Tenant Owners can see all user members across all users.

## User experience
- New "Users" menu option in the Organization section of the shared navigation.
- Users page at `/admin/users` displays a list of all users with an option to add a new user.
- Selecting a user opens a side menu showing user details (name, description) and user members.
- User management options are conditionally displayed based on user permissions:
  - Tenant Owners see all management options.
  - User Admins see member management options but not user configuration options.
  - Regular members see only the user details and member list.
- Simple forms for user creation and editing with validation for name uniqueness.
- Member management interface with clear indication of admin status.
- Confirmation dialogs for sensitive actions like removing members or deleting users.
- All operations strictly scoped to the current tenant to maintain isolation.

## Slices overview
This is a very high-level overview of the slices needed to implement the feature.

### 1. Backend for user management
This slice implements the core user functionality including user aggregate, domain model, database migration, CRUD operations, API endpoints, and tests. It enables creating, updating, and deleting users with proper validation and permission checks.

### 2. Frontend for user management
This slice implements the Users page UI with the ability to list, create, edit, and delete users using the API from the first slice. It includes the navigation menu item and basic user management interface.

### 3. Backend for user membership management
This slice implements user membership functionality including the user member entity, domain model, database migration, CRUD operations for managing user members and admins, API endpoints, and tests. It enforces the permission rules for who can manage user membership.

### 4. Frontend for user membership management
This slice implements the UI for managing user members, including viewing user member lists, adding and removing members, and promoting members to admin status. It respects the visibility rules so users only see appropriate information based on their permissions.