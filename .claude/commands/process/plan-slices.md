---
description: Break down all slices in a feature into detailed technical tasks
argument-hint: Feature ID
---

# Plan Slices Workflow

Feature ID: $ARGUMENTS

Read the feature provided in the arguments above and identify ALL slices listed in it. Your job is to break down each slice into detailed technical tasks.

## Mandatory Preparation

**Read [Product management guide](/.claude/rules/product-management/product-management-guide.md)** to understand your `PRODUCT_MANAGEMENT_TOOL`, `Terminology Mapping`, and `Status Mapping`. Then read the feature from `[PRODUCT_MANAGEMENT_TOOL]`.

## Workflow

Follow the steps below to create tasks for all slices.

### Step 1: Read feature

Read the feature to identify all slices.

### Step 2: Research and plan tasks for all slices

- Read codebase, rule files, and existing patterns
- Break down each slice into tasks (numbered headers) and subtasks (checkbox bullets)
- Each task = one commit = one vertical slice
- Follow existing naming conventions and structure

**CRITICAL: A task = one commit = one complete vertical slice**

### Vertical slice definition:

**Backend vertical slice must include:**
- Database changes (if needed)
- Domain model/aggregate changes
- A single command or query implementation
- API single endpoint
- API tests
- All in ONE task that results in ONE commit

**Frontend vertical slice must include:**
- Component creation
- API integration (TanStack Query)
- State management
- User interactions
- All in ONE task that results in ONE commit

**WRONG approach (multiple tasks/commits):**
- Task 1: Add database field ❌
- Task 2: Update domain model ❌
- Task 3: Create command ❌
- Task 4: Add API endpoint ❌
- Task 5: Write tests ❌

**RIGHT approach (vertical slice):**
- Task 1: Create complete GetUser query with repository method, query handler, API endpoint, and tests ✅

Since we prefer API tests, a commit with tests is a full vertical slice.

### Task guidelines:
- The first task for a new feature is often larger (includes migration, aggregate, command, endpoint, tests)
- Subsequent tasks can be smaller (e.g., just adding a query with its endpoint and tests)
- Keep CRUD operations separate: Create in one task, Read in another, Update in another, Delete in another
- Never have multiple commands or queries in one task
- Never have multiple API endpoints in one task
- Prefix each task with a level-2 header followed by [Planned], for example `## 1 Create GetUser query [Planned]`
- Use "Sentence case" not "Title Case" for task headers

> Note: Each task must result in code that compiles, runs, and can be tested independently

### Step 3: Show task structure to user for approval

Display ALL slices with their tasks and subtasks.

Format: Slice labels, numbered tasks with markdown headers, and bullet subtasks (no status tags)

Example format:
```
Slice 1: Frontend UI skeleton for user management

## 1 Create Users page with navigation and route
- Add Users navigation menu item
- Create Users page route file
- Create UsersPage component structure

## 2 Create users table with mock data
- Create mock users data
- Create UsersTable component
- Add table to Users page

Slice 2: Backend for user CRUD operations

## 1 Create user aggregate, command, endpoint, migration, and tests
- Create `UserId` strongly typed ID
- Create `User` aggregate
- Create `IUserRepository` interface and implementation
- Create `UserConfiguration` for EF Core
- Create database migration
- Create `CreateUser` command and handler
- Create API endpoint for create user
- Create API tests for create user
```

Ask "Does this task structure look good?" (Yes/No)
- If No: Ask what to change, update task structure, show again, repeat approval
- If Yes: Continue to Step 4

### Step 4: Create detailed tasks in `[PRODUCT_MANAGEMENT_TOOL]`

For each slice:
- Get the slice from `[PRODUCT_MANAGEMENT_TOOL]` (created in create-prd workflow Step 7)

For each slice task:
- Create in `[PRODUCT_MANAGEMENT_TOOL]` with:
  - Title: "[Task title]" (e.g., "1 Create Users page with navigation and route")
  - Description: Detailed implementation notes with checkbox list of slice subtasks
  - Link to parent slice
  - Assign to: "me"

**Note:** If any required information is unclear (team, state, etc.), ask the user.

**If `[PRODUCT_MANAGEMENT_TOOL]` is "Markdown":**

For each slice:

1. **Create slice file:**
   - Path: `./.workspace/task-manager/yyyy-MM-dd-[feature-title]/[#]-[slice-title].md`
   - Example: `1-backend-for-user-management.md`, `2-frontend-for-user-management.md`

2. **Add slice header and description:**
   - Level 1 header: Slice title from the PRD (e.g., "Backend for user management")
   - **Purpose:** Short description of what this slice delivers
   - **NOT included:** Out-of-scope items
   - **Dependencies:** Previous slices or external requirements
   - **IMPORTANT:** Warning not to work outside this scope

3. **Add tasks:**
   - Add slice task headers with [Planned] status (format: `## 1 Task title [Planned]`)
   - Add detailed implementation notes with checkbox slice subtasks (format: `- [ ] Subtask description`)

### Subtask guidelines:

**BE PRECISE about:**
- Class names, method names, property names
- API endpoints and routes
- Validation rules and business logic
- Database schema and relationships
- What guards and permissions to check

**DON'T INCLUDE:**
- Actual code syntax or snippets
- Specific implementation details that might change
- Technology-specific configurations
- Detailed error messages

**Good subtask example:**
```
- [ ] Create `UpdateUser` command
- [ ] Validate name length (≤ 50 Unicode chars)
- [ ] Guard tenant Owner permission only
- [ ] Check name uniqueness via repository
```

**Bad subtask example (too much code detail):**
```
- [ ] Create UpdateUser command
- [ ] Add RuleFor(x => x.Name).Length(1, 50).WithMessage("...")
- [ ] if (executionContext.UserInfo.Role != UserRole.Owner) return Result.Forbidden()
```

**Additional guidelines:**
- For each task add small subtasks for each implementation step
- Focus on WHAT to implement, not HOW to code it
- Let the implementation phase consult rules for exact patterns
- Use `backticks` for names of classes, properties, file names
- Avoid modifying `shared-kernel` or `shared-webapp` unless explicitly agreed

### Step 5: Inform user

Inform the user: Tasks have been created successfully.

## Tools available

- Perplexity MCP tool for research on complex implementation questions
- Context7 MCP tool for up-to-date best practices and technology updates

## Examples

Using these examples to understand how to write tasks and subtasks for slices:

- [Backend for user management](/.claude/samples/1-backend-sample.md)
- [Frontend for user management](/.claude/samples/2-frontend-sample.md)

## ✅ DO:

- Keep tasks focused on the smallest vertical slices with API tests and one commit per task
- Show task structure to user for approval before creating detailed descriptions

## ❌ DON'T:

- Skip research – always read existing code and rules before writing tasks and subtasks
- Combine multiple commands or queries into one task or combine frontend and backend tasks into one
- Create separate tasks for tests. Tests should be part of the task
- Create tasks that cannot be tested in isolation (for example, a command without an API endpoint)
- Include implementation details that cannot be built or tested in one commit
- Create tasks that depend on details that will be implemented later
- Modify `shared-kernel` or `shared-webapp` without explicit agreement
- Write titles in Title Case. Instead always use sentence case

**SERIOUSLY:**
Do the research. Read code and rule files. Create excellent tasks. No shortcuts.
