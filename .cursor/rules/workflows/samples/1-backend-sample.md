# Backend for team management

**Purpose:**

This increment implements the core team functionality including:
- Team aggregate and domain model
- Database migration for the Teams table
- CRUD operations for teams
- API endpoints for team management
- API tests for all operations with proper validation and permission checks

**NOT included:**
- Frontend implementation (next increment)
- Team member management (separate increment)
- User interface components
- Authentication/authorization UI

**Dependencies:**
- None (this is the first increment)

**IMPORTANT:**
Do not implement frontend components or team membership functionality in this increment.

## CRITICAL: Vertical slice requirements

**EACH TASK = ONE COMMIT = ONE VERTICAL SLICE**

A vertical slice means:
- Database changes + backend logic + API endpoint + tests = ONE task/commit
- The code must compile, run, and be testable after EACH task
- Never split database, backend, API, and tests into separate tasks

**Example of WRONG approach (multiple commits):**
- Task 1: Create database migration ❌
- Task 2: Create Team aggregate ❌  
- Task 3: Create CreateTeam command ❌
- Task 4: Create API endpoint ❌
- Task 5: Create tests ❌

**Example of RIGHT approach (vertical slice):**
- Task 1: Create team aggregate, command, endpoint, migration and tests ✅
  (Everything needed to create a team in ONE commit)

---

## 1 Create team aggregate, command, endpoint, migration and tests [Planned]

1.1 Create `Team` aggregate
  - Create `TeamId` strongly typed ID
  - Define properties `TeamId`, `Name`, `Description`, `TenantId`
  - Add `Create` factory method with validation and state updates
  - Make `Team` implement `ITenantScoped` so all repository methods are scoped to the tenant

1.2 Create `TeamRepository` inheriting from `ICrudRepository`

1.3 Configure strongly typed IDs via `TeamConfiguration`

1.4 Create database migration for `Teams` table
  - Add unique index on `TenantId` and `Name`

1.5 Create `CreateTeam` command
  - Validate name length (≤ 50 Unicode chars)
  - Guard tenant `Owner` permission only (not Admin)
  - Guard uniqueness via `GetByName`
  - Use `IExecutionContext.TenantId!` for tenant
  - Track `TeamCreatedEvent` with `TeamId` as property (`TenantId` and `UserId` are added automatically)

1.6 Create API endpoint `POST /api/account-management/teams`

1.7 Create API tests

## 2 Create GetTeam query [Planned]

2.1 Add `GetByName` to `TeamRepository`

2.2 Create `GetTeam` query

2.3 Create API endpoint `GET /api/account-management/teams/{teamId}`

2.4 Create API tests

## 3 Create GetTeams query [Planned]

3.1 Add `GetAll` repository method to `TeamRepository`

3.2 Create `GetTeams` query

3.3 Create API endpoint `GET /api/account-management/teams`

3.4 Create API tests

## 4 Create UpdateTeam command [Planned]

4.1 Add `Update` method on `Team` aggregate

4.2 Create `UpdateTeam` command
  - Validate name length (≤ 50 Unicode chars)
  - Guard tenant `Owner` permission only (not Admin)
  - Guard uniqueness via `GetByName`

4.3 Create API endpoint `PUT /api/account-management/teams/{teamId}`

4.4 Create API tests

## 5 Create DeleteTeam command [Planned]

5.1 Create `DeleteTeam` command
  - Guard tenant `Owner` permission only (not Admin)
  - Track `TeamDeletedEvent`

5.2 Create API endpoint `DELETE /api/account-management/teams/{teamId}`

5.3 Create API tests