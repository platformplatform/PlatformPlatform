---
description: Create a product requirement description (PRD) for a new feature
---

# Create PRD Workflow

Your job is to work with the user through an interactive wizard to create a high-level PRD using language that is easy to understand for non-technical people. The workflow creates a PRD with all product increments in EITHER `[PRODUCT_MANAGEMENT_TOOL]` OR Markdown files (user's choice).

**Note:** For terminology mapping between generic terms (PRD, product increment, etc.) and `[PRODUCT_MANAGEMENT_TOOL]`, see [Product management terminology mapping](/.claude/rules/main.md#product-management-terminology-mapping).

## Workflow

Follow the steps below to create the PRD.

### Step 1: Choose your product management tool

Ask "Do you want to use `[PRODUCT_MANAGEMENT_TOOL]` or Markdown files for tracking this PRD?" (`[PRODUCT_MANAGEMENT_TOOL]`/Markdown files)

**If user chooses Markdown files:**
- If `.workspace/task-manager` does not exist, run: `dotnet run --project developer-cli -- init-task-manager`

**If user chooses `[PRODUCT_MANAGEMENT_TOOL]`:**
- Call any MCP command to check if `[PRODUCT_MANAGEMENT_TOOL]` is authenticated
- If not available or authentication fails: Stop workflow, tell user to check `[PRODUCT_MANAGEMENT_TOOL]` configuration in [Product management tool](/.claude/rules/main.md#product-management-tool)

### Step 2: Ask for feature description

Use the AskUserQuestion tool to ask the user what feature they want to build:

```
AskUserQuestion with:
- question: "What feature would you like to build?"
- header: "Feature"
- multiSelect: false
- options:
  - label: "New feature", description: "Create a new feature"
  - label: "Enhancement", description: "Enhance existing functionality"
```

Users will typically use the custom text option to describe their actual feature.

**If the user's answer comes back empty:**
- Tell the user to enable Plan Mode and try again
- STOP the workflow

**If you receive a valid answer:**
- Use the text they entered as the feature description

### Step 3: Research and understand the feature

Conduct deep research for a feasible solution that takes the existing codebase and features into consideration:
- Understand the user's requirements and business context
- Investigate the current state and implementation in the codebase:
  - Specify which self-contained system (e.g., `account-management`, `back-office`) the feature belongs to
  - Respect the multi-tenant nature: design features to work for one tenant by default, unless otherwise specified
- Use MCP tools (like context7 for library docs), Perplexity for online research, or web research for best practices and technologies
- Read relevant code files and rule files to understand patterns and conventions

### Step 4: Interactive requirements wizard

Now that you've done research, ask the user ALL relevant questions to understand their requirements.

You should ask as many questions as make sense for the feature. Create thoughtful questions based on your research. Be creative and comprehensive - the goal is to gather all the insights needed to create an excellent PRD.

**Ask these required questions using AskUserQuestion:**

**Self-contained system:**
```
AskUserQuestion with (put the most likely SCS first):
- question: "Which self-contained system (SCS) should this feature belong to?"
- header: "SCS"
- multiSelect: false
- options:
  - label: "account-management", description: "Tenant and user management system"
  - label: "back-office", description: "Support and system admin tools"
  - label: "[Suggested SCS based on research]", description: "Based on my analysis"
```

**IMPORTANT:** Based on your research and the nature of the feature, ask 2-6 additional relevant questions that would help you create a better PRD. Examples:
- "What user roles should have access to this feature?"
- "Should this be tenant-specific or system-wide?"
- "Are there any specific performance requirements?"
- "Should this integrate with existing [X] functionality?"
- "What level of complexity: simple CRUD, workflow-based, or complex business logic?"

Use your judgment to ask additional questions using AskUserQuestion.

**E2E tests:**
```
AskUserQuestion with:
- question: "Should this PRD include Playwright end-to-end tests?"
- header: "E2E Tests"
- multiSelect: false
- options:
  - label: "Yes", description: "Include E2E tests as a separate product increment"
  - label: "No", description: "Skip E2E tests for now"
```

**Parallel optimization:**
```
AskUserQuestion with:
- question: "Should product increments be optimized for parallel work of backend and frontend?"
- header: "Parallel"
- multiSelect: false
- options:
  - label: "Yes", description: "Backend and frontend with mocks work in parallel, then integration"
  - label: "No", description: "Sequential approach: backend first, then frontend"
```

**Frontend-first approach (only ask if user selected "No" for parallel optimization):**
```
AskUserQuestion with:
- question: "Should we create frontend mockups first for UI/UX exploration?"
- header: "Approach"
- multiSelect: false
- options:
  - label: "Yes", description: "Frontend mockups first to validate UI/UX before backend"
  - label: "No", description: "Backend-first approach"
```

### Step 5: Draft the complete PRD and get approval

Based on all the research and user answers, draft the complete PRD.

**Create the PRD content following the [example PRD structure](/.claude/samples/example-prd.md):**

1. **High-level PRD description:**
   - Use sentence case for level-1 headers
   - Stay at a high level—no implementation details or code examples
   - Use correct domain terminology: multi-tenant, self-contained system, shared kernel, tenant, user, etc.
   - Specify which self-contained system(s) are in scope
   - Avoid repetition

2. **Product increments section** structured based on wizard answers:

   **Examples based on common patterns:**

   **Example 1 - Parallel optimization (small feature):**
   - Frontend with mocked API responses
   - Backend implementation with real data (can work in parallel)
   - Integration (connect frontend to real backend, remove mocks)
   - E2E tests (if E2E tests selected)

   **Example 2 - Frontend-first approach:**
   - Frontend mockups/prototypes with static data
   - Backend implementation based on frontend contract
   - Integration (connect frontend to backend)
   - E2E tests (if E2E tests selected)

   **Example 3 - Backend-first approach:**
   - Backend implementation
   - Frontend implementation
   - E2E tests (if E2E tests selected)

   **Example 4 - Backend-only feature:**
   - Backend implementation (API endpoints, commands, queries, migrations, tests)

   **Example 5 - Large feature with multiple cycles:**
   - Frontend core UI with mocks
   - Backend core functionality
   - Integration frontend with backend
   - Frontend advanced features with mocks
   - Backend advanced functionality
   - Integration frontend with backend
   - E2E tests (if E2E tests selected)

   **Note:** These are examples only. Adapt the product increment structure to match the actual feature requirements, scope, and user answers.

3. **Product increment guidelines:**
   - Each product increment should be a logical grouping (e.g., "all backend", "all frontend", "all e2e tests")
   - Keep product increments small enough to review effectively
   - Write a clear paragraph describing what each product increment delivers
   - Each task = one commit = one vertical slice
   - All tasks in an increment work together to deliver a coherent feature

Show the complete PRD to the user - display the full content including all product increments with their descriptions.

**Ask for approval:** "Does this PRD look good?" (Yes/No)
- If No: Ask what to change, update the PRD content, show again, repeat approval
- If Yes: Continue to Step 6

### Step 6: Confirm PRD name and details

Extract PRD name from feature description, remove imperative verbs ("Create", "Add", "Implement"), convert to sentence case:
- Examples: "Redesign user interface" → "Userinterface redesign", "Implement SSO Authentication" → "SSO authentication"

Ask "PRD name: '[name]' - correct?" (Yes/Custom)

**If user chose `[PRODUCT_MANAGEMENT_TOOL]` in Step 1:**
- Ask "Move to active work?" (Yes (Now)/No (Later))

### Step 7: Create PRD in product management tool

**If user chose `[PRODUCT_MANAGEMENT_TOOL]` in Step 1:**

1. **Create PRD:**
   - Name: [confirmed PRD name]
   - Assign to: "me"

2. **Create product increments:**
   - For each product increment: Create with title=[product increment title], description=[product increment description]
   - Link to PRD, assign to "me"

3. **If "Move to active work" was "Yes (Now)" in Step 6:**
   - Update PRD and all product increments to active work

**If user chose Markdown files in Step 1:**

1. **Create directory:**
   - Path: `./.workspace/task-manager/yyyy-MM-dd-[prd-title]/`
   - Use today's date in yyyy-MM-dd format
   - Use PRD title in kebab-case
   - Example: `./.workspace/task-manager/2025-10-25-user-management/`

2. **Create PRD file:**
   - Filename: `prd.md` (always this exact name)
   - Content: Complete approved PRD content

**Inform user:** The PRD has been created.

### Step 8: Ask about creating tasks

Ask "Should I create detailed tasks now?" (Yes/No)

**If user selects "Yes":**
- Use the SlashCommand tool to call: `/process:create-tasks`

**If user selects "No":**
- Inform the user they can run `/process:create-tasks` later when ready

## Guidelines

✅ DO:
- Follow the exact structure in the example PRD
- Conduct deep research by reading code, consulting rule files, and using MCP tools
- Specify the self-contained system for the feature
- Respect multi-tenant design by default
- Keep the PRD high level without code snippets
- Ask comprehensive questions in Step 4 to gather all requirements
- Show PRD for approval (Step 5) before creating anything
- Use the AskUserQuestion tool for all wizard questions in Plan Mode

❌ DON'T:
- Write PRDs as user stories—use the example structure
- Include implementation details or code examples in the PRD
- Skip research. Always understand the problem first
- Ignore rule files
- Repeat information across sections
- Write titles in Title Case—use sentence case
- Create `[PRODUCT_MANAGEMENT_TOOL]` entities before getting PRD approval in Step 5
- Rename the file—must be `prd.md`
- Save questions in the PRD file
- Create product increments that split tests, implementation, and migrations across separate increments
- Ask the user clarifying questions before Step 4

**SERIOUSLY:**
Do the research. Read code and rule files. Ask comprehensive questions. Create excellent PRDs. No shortcuts.
