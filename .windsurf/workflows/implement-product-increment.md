---
description: Workflow for implementing tasks for a product increment
---

# Implement Product Increment Workflow

The user will provide a link to files with a PRD and a product increment description, or links to a backlog item in [PRODUCT_MANAGEMENT_TOOL] with the PRD and a link to a product increment. Your job is to implement the tasks defined in the product increment. If no product increment is provided, inspect all product increments to find the first product increment that has not been fully completed.

The product increment contains tasks with several subtasks that must be implemented one by one. After a task and all its subtasks are implemented, you must build and test the code, and mark each subtask as completed `[x]`. Before proceeding to the next task, it's crucial that you ask the user for approval and commit the changes.

## Workflow

Follow these steps which describe in detail how you must implement the tasks in the provided product increment.

1. Review rules files

  Before implementing each task, review the relevant rules thoroughly:

  - For **backend tasks**:
    - Review all the [Backend](/.windsurf/rules/backend) rule files.
  - For **frontend tasks**:
    - Review all the [Frontend](/.windsurf/rules/frontend) rule files.

These rules define the conventions and must be strictly adhered to during implementation.

2. Research codebase

   Research the existing codebase to fully understand:

   - Where the new feature or changes should be implemented.
   - How similar features are typically structured and implemented.

   **Important:**  
   - Rules defined in the rules files always trump existing code if there's any conflict.
   - Do not skip or shortcut this step—it ensures implementation consistency.

3. Implement tasks

   Implement each task in the product increment document **one by one**, strictly following the subtasks listed.

   For every task:

   - Clearly mark each subtask as completed `[x]` after finishing it and **before** moving to the next subtask.
   - Write detailed, clean, production-quality code following conventions from rules and existing code patterns.
   - Regularly reference rules and previous implementation research.
   - Ensure each subtask is thoroughly completed and checked off before proceeding.

4. Validate implementation

   After completing **all subtasks** in a **backend** task:

   - Build the code using Developer CLI (see [Tools](/.windsurf/rules/tools.md)).
   - Verify all existing tests pass.
   - Write new tests to ensure new code coverage, including all edge cases.
   - Run build and tests again until all tests pass.
   - Format the code (see [Tools](/.windsurf/rules/tools.md)).

   After completing **all subtasks** in a **frontend** task:

   - Build the code using Developer CLI (see [Tools](/.windsurf/rules/tools.md)).
   - Run code inspections and formatting (see [Tools](/.windsurf/rules/tools.md)).

   **Do not skip or shortcut these validation steps.**

5. User confirmation and commit

   **Before moving to the next task**:

   - Ask the user for approval.
   - After user approval, commit the implemented code according to [Git Commits](/.windsurf/workflows/git-commits.md).

   **Important:**  
   - Do not proceed without user confirmation.
   - Do not forget to commit code after receiving user confirmation.

6. Repeat process for next task

   - Repeat Steps 1 to 5 for each subsequent task in the product increment.

## Example

✅ DO:
- Review all relevant rule files before each task.
- Thoroughly research existing code implementations and structures before starting each task.
- Implement subtasks sequentially, marking each as completed `[x]` immediately after implementation.
- Always validate backend code (build, test, format) thoroughly after completing each task.
- Always request explicit user confirmation before proceeding to the next task.
- Always commit code immediately after receiving user confirmation.
- Ensure each commit is a deployable and tested unit of work.
- Revisit rules files and research between each task implementation.

 ❌ DON'T:
- Skip reviewing the rules files before implementing tasks.
- Forget to research existing codebase implementations.
- Implement multiple tasks or subtasks simultaneously.
- Proceed to the next subtask or task before marking the current one as completed `[x]`.
- Proceed to the next task without validating builds, tests, and formatting.
- Skip writing and running comprehensive tests, including edge cases.
- Forget to ask for explicit user confirmation before moving on.
- Proceed without committing after user confirmation.
- Continue without following explicit rules defined in [Rules](/.windsurf/rules).

## Example Implementation Flow (Product Increment):

Using the provided `1-backend-sample.md` example:

- **Task 1: Create Team aggregate, command, endpoint, migration, and tests**
  - Review backend rules in [Backend](/.windsurf/rules/backend).
  - Research existing aggregates, commands, and API structures.
  - Implement each subtask sequentially:
    - `[x] 1.1 Create Team aggregate`  
      (Complete the aggregate implementation according to rules.)
    - `[x] 1.2 Create TeamRepository`  
      (Complete repository implementation according to rules.)
    - `[x] 1.3 Configure strongly typed IDs`  
      (Complete configuration according to rules.)
    - `[x] 1.4 Database migration`  
      (Implement migration according to rules.)
    - `[x] 1.5 CreateTeam command`  
      (Implement command with required guards and validations.)
    - `[x] 1.6 API endpoint POST /teams`  
      (Implement API endpoint adhering to conventions.)
    - `[x] 1.7 API tests`  
      (Write and pass tests covering the endpoint.)
  - After subtasks:
    - Build, test, format the backend thoroughly.
    - Request user confirmation.
    - Commit changes after confirmation.

- **Task 2: Create GetTeam query**
  - Repeat from Step 1.

- Continue similarly for each remaining task in the product increment.

**This process ensures a rigorous, thorough, and user-confirmed workflow for high-quality and reliable implementation.**
