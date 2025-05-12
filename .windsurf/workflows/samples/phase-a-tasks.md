---
---

# Phase A: Backend for team management

## Phase description

**Purpose:** This phase implements the core backend functionality for managing teams, including
- Team aggregate and related domain models
- Database migration for the Teams table
- CRUD operations for teams
- API endpoints for team management
- API tests for all operations

**NOT included:**
- Frontend implementation (Phase B)
- Team member management (Phase C)
- User interface components (Phase B)
- Authentication/authorization UI (other phases)

**Dependencies:**
- None (this is the first phase)

**IMPORTANT:**
Do not implement frontend components or team member functionality in this phase.  

## 1 Create team aggregate, command, endpoint, migration and tests

[ ] 1.1 Create `Team` aggregate
  - Create `TeamId` strongly typed ID
  - Define properties `TeamId`, `Name`, `Description`, `TenantId`
  - Add `Create` factory method with validation and state updates
  - Make `Team` implement `ITenantScoped` so all repository methods are scoped to the tenant

[ ] 1.2 Create `TeamRepository` inheriting from `ICrudRepository`

[ ] 1.3 Configure strongly typed IDs via `TeamConfiguration`

[ ] 1.4 Create database migration for `Teams` table
  - Add unique index on `TenantId` and `Name`

[ ] 1.5 Create `CreateTeam` command
  - Validate name length (≤ 50 Unicode chars)
  - Guard tenant `Owner`/`Admin`
  - Guard uniqueness via `GetByName`
  - Use `IExecutionContext.TenantId!` for tenant
  - Track `TeamCreatedEvent` with `TeamId` as property (`TenantId` and `UserId` are added automatically)

[ ] 1.6 Create API endpoint `POST /api/account-management/teams`

[ ] 1.7 Create API tests  

## 2 Create GetTeam query

[ ] 2.1 Add `GetByName` to `TeamRepository`

[ ] 2.2 Create `GetTeam` query

[ ] 2.3 Create API endpoint `GET /api/account-management/teams/{teamId}`

[ ] 2.4 Create API tests  

## 3 Create GetTeams query

[ ] 3.1 Add `GetAll` repository method to `TeamRepository`

[ ] 3.2 Create `GetTeams` query

[ ] 3.3 Create API endpoint `GET /api/account-management/teams`

[ ] 3.4 Create API tests  

## 4 Create UpdateTeam command

[ ] 4.1 Add `Update` method on `Team` aggregate

[ ] 4.2 Create `UpdateTeam` command
  - Validate name length (≤ 50 Unicode chars)
  - Guard tenant `Owner`/`Admin`
  - Guard uniqueness via `GetByName`

[ ] 4.3 Create API endpoint `PUT /api/account-management/teams/{teamId}`  

[ ] 4.4 Create API tests  

## 5 Create DeleteTeam command

[ ] 5.1 Create `DeleteTeam` command
  - Guard tenant `Owner`/`Admin`
  - Track `TeamDeletedEvent`

[ ] 5.2 Create API endpoint `DELETE /api/account-management/teams/{teamId}`

[ ] 5.3 Create API tests
