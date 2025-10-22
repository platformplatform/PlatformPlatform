---
description: Create a product requirement description (PRD) for a new feature
---

# Create PRD Workflow

Your job is to work with the user through an interactive wizard to create a high-level PRD using language that is easy to understand for non-technical people.

The output will be a `./.workspace/task-manager/yyyy-MM-dd-[prd-title]/prd.md` file with the high-level PRD description and overview of product increments needed to implement the feature. Use a tool to get today's date in yyyy-MM-dd format for the directory name.

## Workflow

Follow the steps below to create the PRD.

### Step 1: Ask for feature description

**Use the AskUserQuestion tool to ask the user what feature they want to build:**

```
AskUserQuestion with:
- question: "What feature would you like to build?"
- header: "Feature"
- multiSelect: false
- options:
  - label: "New feature", description: "Create a new feature"
  - label: "Enhancement", description: "Enhance existing functionality"
```

**Note**: Users will typically use the custom text option to describe their actual feature.

**If the user's answer comes back empty**: Tell the user to enable Plan Mode and try again. STOP the workflow.

**If you receive a valid answer:**
- Use the text they entered as the feature description
- Continue to Step 2

### Step 2: Ensure `task-manager` git repository exists

Run `dotnet run --project developer-cli init-task-manager` from the source code folder to initialize the task-manager directory in .workspace as a separate git repository.

### Step 3: Research and understand the feature

Conduct deep research for a feasible solution that takes the existing codebase and features into consideration. This includes:
- Understanding the user's requirements and business context
- Investigating the current state and implementation in the codebase:
  - Specify which self-contained system (e.g., `account-management`, `back-office`) the feature belongs to
  - Respect the multi-tenant nature: design features to work for one tenant by default, unless otherwise specified
- Using MCP tools (like context7 for library docs) or web research for best practices and technologies
- Reading relevant code files and rule files to understand patterns and conventions
- **DO NOT ask the user clarifying questions yet** - save them for Step 4

### Step 4: Interactive requirements wizard

**Now that you've done research, ask the user ALL relevant questions to understand their requirements.**

You should ask as many questions as make sense for the feature. Create thoughtful questions based on your research. **Be creative and comprehensive** - the goal is to gather all the insights needed to create an excellent PRD.

**At minimum, ask these questions using AskUserQuestion:**

**Question 1 - Self-contained system:**
```
AskUserQuestion with:
- question: "Which self-contained system (SCS) should this feature belong to?"
- header: "SCS"
- multiSelect: false
- options:
  - label: "account-management", description: "Tenant and user management system"
  - label: "back-office", description: "Support and system admin tools"
  - label: "[Suggested SCS based on research]", description: "Based on my analysis"
  - label: "New SCS", description: "This feature needs a new self-contained system"
```

**Question 2 - E2E tests:**
```
AskUserQuestion with:
- question: "Should this PRD include end-to-end tests?"
- header: "E2E Tests"
- multiSelect: false
- options:
  - label: "Yes", description: "Include E2E tests as a separate product increment"
  - label: "No", description: "Skip E2E tests for now"
```

**Question 3 - Parallel optimization:**
```
AskUserQuestion with:
- question: "Should product increments be optimized for parallel work?"
- header: "Parallel"
- multiSelect: false
- options:
  - label: "Yes", description: "Backend and frontend with mocks work in parallel, then integration"
  - label: "No", description: "Sequential approach: backend first, then frontend"
```

**Question 4 - Frontend-first approach:**
```
AskUserQuestion with:
- question: "Should we create frontend mockups first for UI/UX exploration?"
- header: "Approach"
- multiSelect: false
- options:
  - label: "Yes", description: "Frontend mockups first to validate UI/UX before backend"
  - label: "No", description: "Backend-first approach"
  - label: "Standard", description: "Backend then frontend (typical flow)"
```

**IMPORTANT**: Based on your research and the nature of the feature, **ask additional questions** that would help you create a better PRD. Examples:
- "What user roles should have access to this feature?"
- "Should this be tenant-specific or system-wide?"
- "Are there any specific performance requirements?"
- "Should this integrate with existing [X] functionality?"
- "What level of complexity: simple CRUD, workflow-based, or complex business logic?"

Use your judgment to ask 2-6 additional relevant questions using AskUserQuestion.

### Step 5: Create the complete PRD automatically

**Based on all the research and user answers, automatically create the complete PRD.**

Create a `./.workspace/task-manager/yyyy-MM-dd-[prd-title]/prd.md` file with:

1. **High-level PRD description** following the [example PRD structure](/.claude/samples/example-prd.md):
   - Use sentence case for level-1 headers
   - Stay at a high level—no implementation details or code examples
   - Use correct domain terminology: multi-tenant, self-contained system, shared kernel, tenant, user, etc.
   - Specify which self-contained system(s) are in scope
   - Avoid repetition

2. **Product Increments section** structured based on wizard answers:

   **If user selected "Parallel optimization = Yes":**
   - Product Increment 1: Backend implementation with real data
   - Product Increment 2: Frontend with mocked API responses (can work in parallel with PI 1)
   - Product Increment 3: Integration (connect frontend to real backend, remove mocks)
   - Product Increment 4: E2E tests (if user selected "Yes" for E2E tests)

   **If user selected "Frontend-first = Yes":**
   - Product Increment 1: Frontend mockups/prototypes with static data
   - Product Increment 2: Backend implementation based on frontend contract
   - Product Increment 3: Integration (connect frontend to backend)
   - Product Increment 4: E2E tests (if user selected "Yes" for E2E tests)

   **If user selected "Standard" or "No" to both:**
   - Product Increment 1: Backend implementation
   - Product Increment 2: Frontend implementation
   - Product Increment 3: E2E tests (if user selected "Yes" for E2E tests)

3. **Product Increment guidelines:**
   - Each product increment should be a logical grouping (e.g., "all backend", "all frontend", "all e2e tests")
   - Keep product increments small enough to review effectively
   - Write a clear paragraph describing what each product increment delivers
   - Each task = one commit = one vertical slice
   - All tasks in an increment work together to deliver a coherent feature

**Save the PRD** to `./.workspace/task-manager/yyyy-MM-dd-[prd-title]/prd.md`.

**Present the PRD to the user** - show them what was created but **DO NOT ask for approval**. The PRD is now complete.

### Step 6: Ask about creating tasks

**Use the AskUserQuestion tool:**

```
AskUserQuestion with:
- question: "Should I create detailed tasks now using /process:create-tasks?"
- header: "Create Tasks"
- multiSelect: false
- options:
  - label: "Yes", description: "Automatically create detailed tasks for all product increments"
  - label: "No", description: "I'll create tasks manually later"
```

**If user selects "Yes":**
- Use the SlashCommand tool to call: `/process:create-tasks [full-path-to-prd.md]`
- Wait for task creation to complete

**If user selects "No":**
- Inform the user they can run `/process:create-tasks [prd-path]` later when ready

### Step 7: Create a backlog item (if configured)

If [PRODUCT_MANAGEMENT_TOOL] is configured, use the MCP tool to create a backlog item named after the PRD with the markdown description.

## Guidelines

✅ DO:
- Follow the exact structure in the example PRD
- Conduct deep research by reading code, consulting rule files, and using MCP tools
- Specify the self-contained system for the feature
- Respect multi-tenant design by default
- Keep the PRD high level without code snippets
- Ask comprehensive questions in Step 4 to gather all requirements
- Automatically create the final PRD without asking for approval
- Use the AskUserQuestion tool for all wizard questions in Plan Mode

❌ DON'T:
- Write PRDs as user stories—use the example structure
- Include implementation details or code examples in the PRD
- Skip research. Always understand the problem first
- Ignore rule files
- Repeat information across sections
- Write titles in Title Case—use sentence case
- Ask for PRD approval—create it automatically after gathering requirements
- Rename the file—must be `prd.md`
- Save questions in the PRD file
- Create product increments that split tests, implementation, and migrations across separate increments
- Ask the user clarifying questions before Step 4

**SERIOUSLY:**
Do the research. Read code and rule files. Ask comprehensive questions. Create excellent PRDs. No shortcuts.
