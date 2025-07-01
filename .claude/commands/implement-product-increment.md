---
description: Coordinates full implementation of a product increment via delegated subagents
---

## Inputs

- `$ARGUMENTS` must contain two space-separated markdown file paths or identifiers:
  1. The PRD markdown
  2. The Product Increment markdown

Example usage:

```
/project:implement-product-increment prd.md increment.md
```
---

## Workflow
Your job is to coordinate the implementation of a product increment. You have a very senior development team, and you're the architect and tech lead.  
You must **not** make any code changes yourself. Stay at a high level and ensure the PRD is implemented as discussed.

Go through each product increment task by task using this workflow:

1. **Delegate development of the task** to a senior developer subagent.
   * Ask the subagent to read `$ARGUMENTS[0]` (PRD) and `$ARGUMENTS[1]` (increment) and prepare a plan for the specific task.
   * Be very clear: they must only implement what's in the task, and not do the entire product increment in one go.
   * Ask the subagent to get an overview of all the rules in `.windsurf/rules` and ask them to read all the relevant rules related to the task. They must always read `.windsurf/rules/main.md`.
   * Ask the developer to read `.windsurf/rules/tools.md` and use the tools while developing:
     - Run `[CLI_ALIAS] build --backend` or `[CLI_ALIAS] build --frontend` (or `[CLI_ALIAS] build` if both are changed) constantly while developing.
     - Run `[CLI_ALIAS] format --backend` or `[CLI_ALIAS] format --frontend` (or `[CLI_ALIAS] format` if both are changed) to clean up code.
     - Run `[CLI_ALIAS] test` to run backend tests and `[CLI_ALIAS] e2e --quiet` to run frontend tests.
   * When feature complete and all tests are passing, run `[CLI_ALIAS] check --backend` (very slow) and `[CLI_ALIAS] check --frontend` to ensure code quality. Fix any issues that are found (re-run the checks).
   * If the developer finds that something in the task needs to be done differently, they should update the current task in `$ARGUMENTS[1]` and prefix with `UPDATED:`, `DELETED:`, `ADDED:`, or `MOVED TO TASK #: `. If they find that something belongs to a different product increment, they should find that product increment in the same folder and update it if it exists. Also, the `$ARGUMENTS[0]` should be updated to reflect the findings.

2. **Stage all changes done by the developer subagent in Git** yourself

3. **Delegate review of the task to another senior developer subagent**
   * Ask the reviewer to read `$ARGUMENTS[0]` (PRD) and `$ARGUMENTS[1]` (increment) and prepare a review for the specific task.
   * This reviewer must:
     - Review uncommitted changes file by file
     - For each file they should find relevant rules in `.windsurf/rules` and make sure the code aligns with the rules.
     - For each file they should also find relevant patterns in the codebase and make sure the code aligns with existing patterns.
     - Return with a detailed list of changes that need to be made, or approval if no changes are needed.
   * Be very clear that the reviewer MUST NOT make any changes to the codebase.
   * If and only if the reviewer approves the implementation, ask them to follow this workflow `.windsurf/workflows/commit-changes.md` and commit the changes. It's important that they do not add any description or co-authors to the commit.

4. **Review checkpoint**  
   * If the changes were NOT approved:
     - Start the process from step 1, but adjust the instructions to address the findings from the review, and provide the findings to the senior developer subagent.
     - Continue the process until the reviewer approves the implementation.
   * If the changes were approved, you must triple check that all tests are passing and code is committed:   
     - Run `[CLI_ALIAS] check` and `[CLI_ALIAS] e2e --quiet`.
     - Confirm that **all application code changes are committed**.
     - IMPORTANT: If any checks fail, start the process from step 1 again. Do NOT proceed to the next task until all checks are passing.

5. **Update PRD and product increment plans**
   * Mark the task as completed in `$ARGUMENTS[1]` using `[x]` (product increment plan)
   * Commit the changes to `$ARGUMENTS[1]` and potentially `$ARGUMENTS[0]` (PRD)
     - These files are in the `/task-manager` directory (a nested git repository, but it's not a submodule)
     - Commit changes inside the submodule using a one line commit message in imperative form

Rinse and repeat these steps until all tasks in `$ARGUMENTS[1]` are completed.

Once a increments are complete, assign a subagent to follow `.windsurf/workflows/prepare-pull-request.md` to prepare the pull request title and description.

When the full product increment is complete, return to me for final verification.
