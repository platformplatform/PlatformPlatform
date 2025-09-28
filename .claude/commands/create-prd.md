---
description: Create a product requirement description (PRD) for a new feature
argument-hint: Brief description of the feature to implement (optional)
---

# Create PRD Workflow

Feature to design: $ARGUMENTS

Start by researching the feature described in the arguments above (if provided). Your job is to work with the user to do research and create a high-level PRD using language that is easy to understand for non-technical people.

The output will be a `./.workspace/task-manager/yyyy-MM-dd-[prd-title]/prd.md` file with the high-level PRD description and overview of product increments needed to implement the feature. Use a tool to get today's date in yyyy-MM-dd format for the directory name.

When approved, create a backlog item in [PRODUCT_MANAGEMENT_TOOL] (if configured) with the PRD as the description.

## Workflow

Follow the steps below to create the PRD.

1. Ensure `task-manager` git repository exists

   Run `[CLI_ALIAS] init-task-manager` to initialize the task-manager directory as a git submodule that's ignored by the main repository.

2. Research and understand the feature

   Conduct research for a feasible solution that takes the existing codebase and features into consideration. This includes:
   - Understanding the user's requirements and business context.
   - Investigating the current state and implementation in the codebase:
     - Specify which self-contained system (e.g., `account-management`, `back-office`) the feature belongs to.
     - Respect the multi-tenant nature: design features to work for one tenant by default, unless otherwise specified.
   - Using MCP tools or other methods for necessary information, best practices, or technologies.
   - Asking clarifying questions if any detail, edge case, or scope is unclear.

3. Create the PRD description

   Based on your research, create a `./.workspace/task-manager/yyyy-MM-dd-[prd-title]/prd.md` file containing the high-level PRD description. Use a tool to get today's date in yyyy-MM-dd format. The `[prd-title]` should be a short, relevant title in kebab-case.

   When writing the PRD description:
   - Use sentence case for level-1 headers.
   - Stay at a high level—no implementation details or code examples.
   - Use correct domain terminology: multi-tenant, self-contained system, shared kernel, tenant, user, etc.
   - Specify which self-contained system(s) are in scope.
   - Avoid repetition.

4. Save and get approval of the PRD description

   Save the PRD description to `./.workspace/task-manager/yyyy-MM-dd-[prd-title]/prd.md` for user review. Present it to the user and iterate until approved before breaking down into product increments.

5. Create product increments overview

   ### Understanding product increments:
   
   A product increment is a collection of related tasks that together form a deployable feature set. One or more product increments can be combined into a single pull request.
   
   **Each product increment contains multiple tasks, where:**
   - Each task = one commit = one vertical slice
   - All tasks in an increment work together to deliver a coherent feature
   - The increment is only complete when ALL its tasks are done
   
   **Example structure:**
   ```
   Product increment 1: Backend for team management
   - Task 1: Create team (aggregate, command, endpoint, migration, tests) 
   - Task 2: Get single team (query, endpoint, tests)
   - Task 3: Get all teams (query, endpoint, tests)
   - Task 4: Update team (command, endpoint, tests)
   - Task 5: Delete team (command, endpoint, tests)
   
   Product increment 2: Frontend for team management
   - Task 1: Teams page with navigation
   - Task 2: Team table with data fetching
   - Task 3: Create team dialog
   - Task 4: Team details side pane
   - Task 5: Edit and delete functionality
   ```

   ### Guidelines for breaking down PRDs:

   - Research the codebase before defining product increments
   - Each product increment should be a logical grouping (e.g., "all backend", "all frontend", "all e2e tests")
   - Keep product increments small enough to review effectively
   - Multiple related product increments can be combined in one pull request
   - For larger features, create separate backend and frontend product increments
   - Avoid mixing backend and frontend in the same product increment
   - Write a clear paragraph describing what each product increment delivers

   Update `./.workspace/task-manager/yyyy-MM-dd-[prd-title]/prd.md` to include a "## Product increments" section at the end.

6. Create a backlog item in [PRODUCT_MANAGEMENT_TOOL] (if configured)

   After final approval, use the [PRODUCT_MANAGEMENT_TOOL] MCP tool to create a backlog item named after the PRD with the markdown description.

## Example

Use this [Product requirement description example](/.windsurf/workflows/samples/example-prd.md) as a template.

✅ DO:
- Follow the exact structure in the example PRD.
- Conduct deep research by reading code, consulting rule files, and using MCP tools or other research methods.
- Specify the self-contained system for the feature.
- Avoid touching `shared-kernel` or `shared-webapp` unless agreed.
- Respect multi-tenant design by default.
- Keep the PRD high level without code snippets.
- Save the PRD for review before approval.
- Use the [PRODUCT_MANAGEMENT_TOOL] MCP tool to create the backlog item after final PRD approval, with the full PRD description.
- Define product increments that are small, deployable feature sets
- Create product increments that can be independently deployed
- Multiple related product increments can be combined in one pull request

❌ DON'T:
- Add details other than description to product increments.
- Write PRDs as user stories—use the example structure.
- Include implementation details or code examples in the PRD.
- Skip research. Always understand the problem first.
- Ignore rule files.
- Repeat information across sections.
- Write titles in Title Case—use sentence case.
- Assume that tools like EF Core are used for database migrations. It's not, and such details should be left to the implementation step.
- Rename the file—must be `prd.md`.
- Delete the PRD file unless the backlog item was created successfully.
- Update the backlog item description differently from the approved PRD.
- Save questions in the PRD file. Always ask clarifying questions in chat.
- Create product increments that split tests, implementation, and migrations across separate product increments

**SERIOUSLY:**  
Do the research. Read code and rule files. Ask questions. No shortcuts.
