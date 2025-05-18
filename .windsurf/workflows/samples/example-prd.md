---
---

# Teams management PRD

A brief high-level introduction: The Teams management feature enables tenants to organize users into named groups with clear membership roles and permissions, allowing for better collaboration and access control within the organization.

## Overview
- Teams exist within a single tenant and have unique names across that tenant.
- **TeamId**: strongly-typed ID with `team` prefix to ensure type safety.
- **Name**: unique per tenant, ≤ 50 Unicode characters to prevent naming conflicts.
- **Description**: ≤ 255 Unicode characters (allow empty string).
- **TenantId**: strongly-typed ID linking the team to its tenant.
- Users can belong to multiple teams within a tenant.
- Team membership has two roles: regular **Member** and **Admin**.
- Team Admins can manage team membership but cannot demote themselves.
- Tenant Owners have full control over all teams and their configuration.
- All users on a tenant can see all teams, but only see team members of teams they belong to.

## Core features
- **Team management**  
  - Creation and deletion of teams by Tenant Owners only.
  - Team name and description can only be modified by Tenant Owners.
  - Teams must have unique names within a tenant.
- **Membership management**  
  - Addition/removal of members by Team Admins or Tenant Owners.
  - Promotion of members to Admin role by Team Admins or Tenant Owners.
  - Team Admins cannot demote themselves from the Admin role.
  - Users can be members of any number of teams.
- **Visibility**  
  - All users can see the list of all teams within their tenant.
  - Only team members can see the member roster of teams they belong to.
  - Tenant Owners can see all team members across all teams.

## User experience
- New "Teams" menu option in the Organization section of the shared navigation.
- Teams page at `/admin/teams` displays a list of all teams with an option to add a new team.
- Selecting a team opens a side menu showing team details (name, description) and team members.
- Team management options are conditionally displayed based on user permissions:
  - Tenant Owners see all management options.
  - Team Admins see member management options but not team configuration options.
  - Regular members see only the team details and member list.
- Simple forms for team creation and editing with validation for name uniqueness.
- Member management interface with clear indication of admin status.
- Confirmation dialogs for sensitive actions like removing members or deleting teams.
- All operations strictly scoped to the current tenant to maintain isolation.

## Product increments overview
This is a very high-level overview of the product increments needed to implement the feature.

### Backend for team management
This increment implements the core team functionality including team aggregate, domain model, database migration, CRUD operations, API endpoints, and tests. It enables creating, updating, and deleting teams with proper validation and permission checks.

### Frontend for team management
This increment implements the Teams page UI with the ability to list, create, edit, and delete teams using the API from the first increment. It includes the navigation menu item and basic team management interface.

### Backend for team membership management
This increment implements team membership functionality including the team member entity, domain model, database migration, CRUD operations for managing team members and admins, API endpoints, and tests. It enforces the permission rules for who can manage team membership.

### Frontend for team membership management
This increment implements the UI for managing team members, including viewing team member lists, adding and removing members, and promoting members to admin status. It respects the visibility rules so users only see appropriate information based on their permissions.
