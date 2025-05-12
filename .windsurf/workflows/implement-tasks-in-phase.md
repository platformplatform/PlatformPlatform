---
description: Workflow for implementing tasks within a phase
---


# Implement tasks within a phase

The user will provide a link to a product requirement description (PRD) or a [Project ID] from [PRODUCT_MANAGEMENT_TOOL], and ask you to implement tasks defined in the provided phase. If no `phase-id` is provided, inspect all phases to find the first phase that has not been completed.

The goal is to implement each task precisely as defined in the phase document, marking each subtask as completed `[x]` before proceeding, verifying builds, tests, formatting, and obtaining user confirmation before committing the changes.

## Workflow

Follow these steps to implement the tasks in the provided phase.

### Step 1: Review Rules Files

**Before implementing each task**, review the relevant rules thoroughly:

- For **backend tasks**:
  - Review all the [Backend](/.windsurf/rules/backend/) rule files.
- For **frontend tasks**:
  - Review all the [Frontend](/.windsurf/rules/frontend/) rule files.

These rules define the conventions and must be strictly adhered to during implementation.

### Step 2: Research Codebase

Research the existing codebase to fully understand:

- Where the new feature or changes should be implemented.
- How similar features are typically structured and implemented.

**Important:**  
- Rules defined in the rules files always trump existing code if there's any conflict.
- Do not skip or shortcut this step—it ensures implementation consistency.

### Step 3: Implement Tasks

Implement each task in the phase document **one by one**, strictly following the subtasks listed.

For every task:

- Clearly mark each subtask as completed `[x]` after finishing it and **before** moving to the next subtask.
- Write detailed, clean, production-quality code following conventions from rules and existing code patterns.
- Regularly reference rules and previous implementation research.
- Ensure each subtask is thoroughly completed and checked off before proceeding.

### Step 4: Validate Implementation

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

### Step 5: User Confirmation and Commit

**Before moving to the next task**:

- Request explicit confirmation from the user.
- After user confirmation, commit the implemented code according to [Git Commits](/.windsurf/workflows/git-commits.md).

**Important:**  
- Do not proceed without user confirmation.
- Do not forget to commit code after receiving user confirmation.

### Step 6: Repeat Process for Next Task

- Repeat Steps 1 to 5 for each subsequent task in the phase.

## ✅ DO:
- Review all relevant rule files before each task.
- Thoroughly research existing code implementations and structures before starting each task.
- Implement subtasks sequentially, marking each as completed `[x]` immediately after implementation.
- Always validate backend code (build, test, format) thoroughly after completing each task.
- Always request explicit user confirmation before proceeding to the next task.
- Always commit code immediately after receiving user confirmation.
- Ensure each commit is a deployable and tested unit of work.
- Revisit rules files and research between each task implementation.

## ❌ DON'T:
- Skip reviewing the rules files before implementing tasks.
- Forget to research existing codebase implementations.
- Implement multiple tasks or subtasks simultaneously.
- Proceed to the next subtask or task before marking the current one as completed `[x]`.
- Proceed to the next task without validating builds, tests, and formatting.
- Skip writing and running comprehensive tests, including edge cases.
- Forget to ask for explicit user confirmation before moving on.
- Proceed without committing after user confirmation.
- Continue without following explicit rules defined in [Rules](/.windsurf/rules/).

## Example Implementation Flow (Phase A Backend):

Using the provided phase-a-tasks.md example:

- **Task 1: Create Team aggregate, command, endpoint, migration, and tests**
  - Review backend rules in [Backend](/.windsurf/rules/backend/).
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

- Continue similarly for each remaining task in the phase.

**This process ensures a rigorous, thorough, and user-confirmed workflow for high-quality and reliable implementation.**
