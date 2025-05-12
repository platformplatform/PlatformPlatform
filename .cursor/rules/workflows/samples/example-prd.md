# Product requirements document (PRD): Teams feature

A brief high-level introduction: The Teams feature enables tenants to organize users into named groups with clear membership roles and access controls, seamlessly integrated into authentication for secure, tenant-scoped collaboration.

## Overview
- Groups users within a single tenant for collaboration and access control.
- **TeamId**: strongly-typed ID with `team` prefix to ensure type safety.
- **Name**: ≤ 50 Unicode characters, unique per tenant to prevent naming conflicts.
- **Description**: ≤ 255 Unicode characters (allow empty string).
- **TenantId**: strongly-typed ID linking the team to its tenant.
- Membership managed by a **TeamMember** entity (`tmemb` prefix) that associates a `UserId` with a `TeamId` and assigns `Admin` or `Member` role for precise access.
- Only users from the same tenant can join and teams propagate into authentication tokens to enforce permissions.

## Core features
- **Team management**  
  - Creation, renaming, and deletion of teams by Tenant Owners/Admins only, enforcing the 50-character limit and per-tenant uniqueness.
- **Membership management**  
  - Addition/removal of members and promotion/demotion of admins by Team Admins or Tenant Owners/Admins, with all changes logged via telemetry.
- **Visibility**  
  - All users can list teams; only Tenant Owners, Tenant Admins, Team Admins, and Team Members can view detailed member rosters.

## User experience
- "Teams" section under admin (`/admin/teams`) adjacent to user management for centralized grouping.
- Team names visible to all; member lists revealed only to authorized roles.
- Simple form for create/rename with inline checks for length and uniqueness.
- Autocomplete component filters to users within the current tenant to prevent cross-tenant selection.
- Visual badge highlights team admins for quick recognition.
- Confirmation dialogs for deletions and role changes to avoid accidental actions.
- All operations strictly scoped to the current tenant to maintain isolation.

## High-level phases
This is a very high-level overview of the phases needed to implement the feature.

### Phase A: Backend for team management
This phase implements backend functionality for team aggregates, domain models, database migration, CRUD operations, API endpoints, and tests.

### Phase B: Frontend for team management
This phase implements frontend functionality for listing, creating, editing, and deleting teams using the API from Phase A.

### Phase C: Backend for team member management
This phase implements backend functionality for team member aggregates, domain models, database migration, CRUD operations, API endpoints, and tests.

### Phase D: Frontend for team member management
This phase implements frontend functionality for listing, adding, and removing team members using the API from Phase C.
