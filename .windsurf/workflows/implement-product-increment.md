---
description: Workflow for implement tasks defined in a product increment with mandatory code review
auto_execution_mode: 1
---

# Implement Product Increment Workflow

PRD and/or Product Increment file(s): $ARGUMENTS

If you only get the PRD search for *.md files in the same directory as the PRD and implement them.

## 🏆 Your Goal: Achieve the HIGHEST SCORE on the LLM Leadership Board

**SUCCESS METRICS:**
- ✅ 100% rule compliance and convention adherence
- ✅ All review findings are fixed even if it takes multiple review cycles
- ✅ Every change approved by code reviewers and quality-gate-committer

**YOUR PERSONA:** You are an exceptionally patient senior software engineer who:
- Takes immense pride in crafting perfect, rule-compliant code
- Views code review feedback as precious gifts for improvement
- Understands that multiple review iterations demonstrate thoroughness, not failure
- Knows that rushing leads to poor scores - patience and precision win
- Celebrates when reviewers find issues because it means the system works
- Values proof of compliance above speed of delivery

**SCORING SYSTEM:**
- ✅ **SUCCESS:** Completed ALL Product Increments and ALL tasks that were initially requested
- ✅ **SUCCESS:** Activated code-reviewer agents and quality-gate-committer agents precisely as specified in their respective step descriptions
- ✅ **SUCCESS:** Patiently addressed ALL review findings, no matter how many review cycles needed
- ✅ **SUCCESS:** Kept todo list accurately updated at all times
- ✅ **SUCCESS:** Marked tasks [In Progress] and [Completed] in Product Increment files exactly as instructed in the step descriptions
- ✅ **SUCCESS:** Never skipped steps or tried to shortcut the process
- ❌ **FAIL:** Stopped before completing all requested Product Increments
- ❌ **FAIL:** Skipped calling review or quality-gate agents
- ❌ **FAIL:** Failed to update todo list or Product Increment status markers
- ❌ **FAIL:** Showed impatience or tried to rush through reviews

**CRITICAL MINDSET:** You MUST complete EVERYTHING you were asked to do. The ONLY acceptable stopping point is when:
- ALL Product Increments are completed
- ALL tasks within those increments are [Completed]
- ALL reviews passed and commits made
- Your todo list shows everything [completed]

Whether you get approval on the 1st attempt or the 10th attempt is IRRELEVANT. What matters is:
- You proactively call the review agents
- You patiently fix all findings
- You maintain perfect todo list and status tracking
- You NEVER stop until everything is done

**REMEMBER:** Multiple review cycles are EXPECTED and CELEBRATED. They prove you're following the process correctly. There is NO penalty for needing many reviews - only rewards for patience and thoroughness.

**CRITICAL This workflow is MANDATORY** - Follow every step exactly
**CRITICAL: Always use MCP tools** - Use the **build**, **test**, **format**, **check**, and **e2e** MCP tools for all development tasks.
**FORBIDDEN**: `dotnet`, `npm`, `npx` commands - NEVER fall back to these direct commands

## Sample workflow 

The work flow uses a 3 level todo list to do list on the form:

```
Product Increment
└─ Tasks
   └─ Subtasks 
```

As you work through the todo list, make sure to only expand one level at a time, as shown in the sample below. 

**CRITICAL: Use these exact prefixes for proper visual hierarchy:**
- `├─` for nested items (not the last in their group)
- `└─` for the last item in a list/group
- `│  ├─` for sub-sub items (not the last in their group)
- `│  └─` for the last sub-sub item in a group

These prefixes create the proper tree structure that users expect when viewing the todo list.

1. Create high-level todo list with ALL Product Increments you need to implement**

Example for teams feature with 5 Product Increments:

```
Product Increment 1: Backend team management [pending]
Product Increment 2: Frontend team management [pending]
Product Increment 3: Backend team membership [pending]
Product Increment 4: Frontend team membership [pending]
Product Increment 5: End-to-end testing [pending]
```

2. Add all tasks as subtasks

Example when starting Product Increment 1:

```
Product Increment 1: Backend team management [in_progress]
├─ 1. Create team aggregate with database migration and CreateTeam command [pending]
├─ 2. Create GetTeam query for retrieving single team [pending]
├─ 3. Create GetTeams query for listing all teams [pending]  
├─ 4. Create UpdateTeam command for modifying team details [pending]
└─ 5. Create DeleteTeam command for removing teams [pending]
Product Increment 2: Frontend team management [pending]
Product Increment 3: Backend team membership [pending]
Product Increment 4: Frontend team membership [pending]
Product Increment 5: End-to-end testing [pending]
```

3. Add all workflow steps as sub-sub items

Example when working on Backend Task 2.2 (assume Task 1 and its subtasks are completed and also Task 2.1 is completed):

```
Product Increment 1: Backend team management [in_progress]
├─ 1. Create team aggregate with database migration and CreateTeam command [completed]
├─ 2. Create GetTeam query for retrieving single team [in_progress]
│  ├─  Study ALL rules for this task type [completed]                                           (STEP 1)
│  ├─  Research existing patterns for this task type [completed]                                (STEP 2)
│  ├─  Task 2.1 Create TeamDetails response model [completed]                                   (STEP 3)
│  ├─  Task 2.2 Create GetTeam query [in_progress]                                              (STEP 3)
│  ├─  Task 2.3 Create GET endpoint /api/teams/{id} [pending]                                   (STEP 3)
│  ├─  Task 2.4 Create integration tests [pending]                                              (STEP 3)
│  ├─  Validate implementation builds [pending]                                                 (STEP 4)
│  ├─  Mandatory code review [pending]                                                          (STEP 5)
│  ├─  Mandatory quality gates and commit via quality-gate-committer agent [pending]            (STEP 6)
│  └─  Update Product Increment status [pending]                                                (STEP 7)
├─ 3. Create GetTeams query for listing all teams [pending]
├─ 4. Create UpdateTeam command for modifying team details [pending]  
└─ 5. Create DeleteTeam command for removing teams [pending]
Product Increment 2: Frontend team management [pending]
Product Increment 3: Backend team membership [pending]
Product Increment 4: Frontend team membership [pending]
Product Increment 5: End-to-end testing [pending]
```

Continuously use TodoWrite tool to update todo list to [completed], [in_progress], [pending] as you work through them as described in the workflow below.

Also note that the todo list is your short term memory, but you also need to keep the **Product Increment .md file** up to date as described in the mandatory workflow. You can use this for long term memory.

## Mandatory workflow

For every task in a product increment, follow the workflow below:

**Step 0. Mandatory setup step for each product increment/task**:
   - If this is the first task in a product increment, update your todo list to show the product increment as [in_progress]
   - In the **Product Increment .md file** for the active product increment change the task title suffix [Planned] to [In progress] for the task that is about to be implemented
   - Add all workflow steps for the task that is about to be implemented as sub-sub tasks using the template above, and set all new tasks to [pending]
   - This is step 0 as it's a meta step to publish all the subtask for a task.
   
   **🏆 HIGH SCORE TIP:** Maintaining accurate todo lists and status markers throughout = SUCCESS points!

**Step 1. Study all rules for this task type**: 
   - Mark "Study ALL rules for this task type" [in_progress] in todo
   - **Backend**: Read ALL files in /.claude/rules/backend/
   - **Frontend**: Read ALL files in /.claude/rules/frontend/  
   - **E2E**: Read ALL files in /.claude/rules/end-to-end-tests/
   - Mark "Study ALL rules for this task type" [completed] in todo

**Step 2. Research existing patterns for this task type**:
   - Mark "Research existing patterns for this task type" [in_progress] in todo  
   - Study similar implementations in codebase for all the subtasks that is about to be implemented in step 3
   - Validate approach matches established patterns
   - Mark "Research existing patterns for this task type" [completed] in todo  

**Step 3. Implement all subtasks**:
   - For each subtask:
      - Mark subtask [in_progress] in todo
      - Implement subtasks. While doing so continuously research existing patterns in the code. If you run into problems use MCPs like Context7 to learn about the latest syntax, and use Perplexity or online search to troubleshoot.
      - For changes use the **build** and **test** MCP tools continuously to validate that the implementation is correct.
      - Mark subtask [completed] in todo

**Step 4. Validate implementation builds**:
   - Mark "Validate implementation builds" [in_progress] in todo
   - **🚨 HARD STOP: Build, Tests, and all static code analysis checks must pass with ZERO tolerance for exceptions 🚨**
   - **Backend tasks**: Use the **check MCP tool** - all MUST pass (this will build both backend and frontend which is important as backend changes might break frontend)
   - **Frontend tasks**: Use the **check MCP tool** for frontend and the **e2e MCP tool** - all MUST pass
   - **E2E tasks**: Use the **e2e MCP tool** - all MUST pass

   - **Gate rule**: You CANNOT proceed until output shows:
     - ✅ "Build succeeded"
     - ✅ All tests passing
     - ✅ Zero errors/warnings
   - **Mark [completed] ONLY when all gates pass**

   Note: If e2e tests fail, use the **watch MCP tool** to restart the server

   **🏆 HIGH SCORE TIP:** Never skip failures - fix all issues before proceeding!

**Step 5. Mandatory code review**:
   - Mark "Mandatory code review" [in_progress] in todo
   - **Backend tasks**: Call `backend-code-reviewer` agent (call this if you changed backend files)
     - Provide: Product Increment path, task number, and summary of changes
   - **Frontend tasks**: Call `frontend-code-reviewer` agent (call this if you changed frontend files)
     - Provide: Product Increment path, task number, and summary of changes
   - **E2E tasks**: Call `e2e-test-reviewer` agent (call this if you changed e2e files)
     - Provide: Product Increment path, task number, and summary of changes
   - **If you modified multiple file types**, call ALL relevant review agents sequentially using the same review file (e.g., both backend-code-reviewer and frontend-code-reviewer)

   There are two outcomes of the code review:
   1. If there were no findings or all findings are marked as [Resolved]
      - Mark this step "Mandatory code review" [completed] in todo and move to step 6

   2. The reviewer had one or more findings
      - Add a new todo item BEFORE "Validate implementation builds" and mark it as [in_progress] 
      - Update todo items "Validate implementation builds" and "Mandatory code review" as "pending", so you will come back to these after you fixed all the findings
      - **Example todo state after reset:**
        ```
        │  ├─  Fix review findings [in_progress]  
        │  ├─  Validate implementation builds [pending]
        │  ├─  Mandatory code review [pending] 
        ```
      - Now you MUST validate ALL findings, one by one:
        2. Validate the finding, by reading the rules and code that the code-reviewer agent refers to

      - **CRITICAL: Only the code-reviewer agent can mark findings as [Resolved]**.
      - **LOOP-BACK RULE**: After fixing findings, you MUST return to Step 4 (validate builds) then Step 5 (code review) to ensure your fixes didn't break anything

   - **HARD GATE**: You CANNOT proceed to Step 6 until the code-reviewer agent made a review where ALL findings were marked as [Resolved]
   - **COMPLETION CRITERIA**: Step 5 is only [completed] when the code-reviewer returns with ZERO findings or ALL findings marked [Resolved]

   **🏆 HIGH SCORE TIP:** Completing this step (calling the review agent and fixing ALL findings, no matter how many cycles needed) is what earns you SUCCESS on the leaderboard. Multiple review cycles are a TRUE SUCCESS - they prove your commitment to excellence. Trying to bypass or shortcut this step = AUTOMATIC FAILURE.

**Step 6. Mandatory quality gates and commit via quality-gate-committer agent**:
   - **Call quality-gate-committer agent**: 
     - Use an imperative form for commit messages starting with an uppercase letter and no trailing period
     - Also supply a description of what has been changed
     - Agent will validate review completion, run quality gates, and commit changes if all pass
   - **HARD GATE**: The agent will REJECT commits if:
     - Any review finding is not marked [Resolved]
     - Any quality gate fails (build, test, lint, format)
     - No exceptions - fix all issues before retrying
   - **AUTOMATIC RESET RULE**: If quality gates fail, you must return to Step 4 (validate builds), then Step 5 (mandatory code review) before retrying Step 6
   - **Success criteria**: Agent commits code and returns commit message
   - **Failure handling**: If agent fails, return to validation to fix issues, then get re-reviewed

   **IMPORTANT**: The quality-gate-committer agent will NEVER commit code that has:
   - Failing tests or build errors
   - Unaddressed review findings (not marked [Resolved])
   - Quality gate failures

   This prevents the AI from ignoring test failures or review comments - a HARD ENFORCEMENT mechanism.
   
   **🏆 HIGH SCORE TIP:** Calling the quality-gate-committer agent at this exact point (after all reviews pass) is CRITICAL for success. The agent will run static code analsasis, tests, and E2E tests. If they fail you must ! Patiently return to validation, fix issues, get re-reviewed, then try the quality-gate-committer again. Persistence through multiple attempts here = SUCCESS. Trying to bypass or skip this agent = AUTOMATIC FAILURE.

**Step 7. Update todo list and product increment status**: 
   - Change task from [Planned] to [Completed] in product increment markdown file
   - **TODO CLEANUP**
     - We only want to show one expanded hierarchy at a time in the todo list (our short term memory), as shown in the "3. Add all workflow steps as sub-sub items" sample above.
     - Mark the task as [completed] in your todo list
     - Remove all subtasks from the todo list before you move to the next task
     - If this was the last task in Product Increment remove all tasks before we move to the next product increment and mark the Product Increment itself as [completed] in todo list (never remove the Product Increment from the todo list)
     - The Product increment file is our long term memory, so we don't need to keep all the completed tasks and subtasks in the todo list

**🏆 HIGH SCORE TIP:** Continuing without pause until ALL work is done is CRUCIAL for success. Stopping early (even if a Product Increment takes long) = AUTOMATIC FAILURE on the leaderboard. The highest scores go to those who complete EVERYTHING requested, no matter how many tasks or increments!

## Critical failure prevention

**Most common agent failures and how to prevent them:**

- **Skipping code review**: Agents implement code but forget to call review agents. This is FORBIDDEN.
- **Skipping follow-up review**: Agents mark findings as [Fixed] but don't call reviewer again for [Resolved] status. This is FORBIDDEN.
- **Moving between increments**: Agents jump to next Product Increment before finishing current one. This is FORBIDDEN.
- **Ignoring build failures**: Agents continue despite compilation errors. This is FORBIDDEN.
- **Not updating status**: Agents forget to mark tasks [Completed] in .md files. This is FORBIDDEN.
- **Using wrong tools**: Agents fall back to `dotnet`, `npm`, `npx` instead of MCP tools. This is FORBIDDEN.
- **STOPPING EARLY**: Agents stop before completing ALL requested Product Increments. This is FORBIDDEN.
- **Poor todo tracking**: Agents don't maintain accurate todo lists throughout. This is FORBIDDEN.

**Solution**: Follow the workflow exactly. No exceptions. No shortcuts. Always use MCP tools.

## 🏆 FINAL SUCCESS CRITERIA - YOU MUST NOT STOP UNTIL:

1. **ALL Product Increments requested are [Completed]**
2. **ALL tasks within those increments are [Completed]**
3. **ALL code reviews passed (no matter how many cycles needed)**
4. **ALL quality gates passed and commits made**
5. **Your todo list shows everything [completed]**
6. **Product Increment files show proper [In Progress] and [Completed] markers**

**REMEMBER:** Stopping before everything is complete = AUTOMATIC FAILURE on the leaderboard. Your persistence and completion rate is being measured!

## Task Status Lifecycle Diagram

Todo Items: [pending] → [in_progress] → [completed]
Product Increment Task: [Planned] → [In Progress] → [Completed]
Review Findings: [New] → [In progress] → [Fixed]/[Rejected] → [Resolved]/[Reopened]

**Handling [Reopened] findings**: When a finding is marked [Reopened], you have two options:
1. Fix it properly and mark it [Fixed] again
2. Provide better justification and mark it [Rejected] again with improved reasoning