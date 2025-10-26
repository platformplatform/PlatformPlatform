---
description: Workflow for create a product requirement description (prd) for a new feature
auto_execution_mode: 1
---

# Create PRD Workflow

Your job is to work with the user through an interactive wizard to create a high-level PRD using language that is easy to understand for non-technical people. The PRD defines a [feature] with all [stories] to be created in `[PRODUCT_MANAGEMENT_TOOL]`.

## Mandatory Preparation

1. **Read [PRODUCT_MANAGEMENT_TOOL]-specific guide** at `/.claude/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand terminology, status mapping, ID format, and MCP configuration.

## Workflow

Follow the steps below to create the PRD.

### Step 1: Initialize `[PRODUCT_MANAGEMENT_TOOL]`

Follow initialization steps in `/.claude/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md`.

### Step 2: Ask what [feature] to build

Use the AskUserQuestion tool to ask the user what [feature] they want to build:

```
AskUserQuestion with:
- question: "What feature would you like to build?"
- header: "Feature"
- multiSelect: false
- options:
  - label: "New feature", description: "Create a new feature"
  - label: "Enhancement", description: "Enhance existing functionality"
```

Users will typically use the custom text option to describe their [feature].

**If the user's answer comes back empty:**
- Tell the user to enable Plan Mode and try again
- STOP the workflow

**If you receive a valid answer:**
- Use the text they entered as the [feature] description for the PRD

### Step 3: Research and understand the [feature]

Conduct deep research for a feasible solution that takes the existing codebase and [features] into consideration:
- Understand the user's requirements and business context
- Investigate the current state and implementation in the codebase:
  - Specify which self-contained system (e.g., `account-management`, `back-office`) the [feature] belongs to
  - Respect the multi-tenant nature: design [features] to work for one tenant by default, unless otherwise specified
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
  - label: "Yes", description: "Include E2E tests as a separate [story]"
  - label: "No", description: "Skip E2E tests for now"
```

**Parallel optimization:**
```
AskUserQuestion with:
- question: "Should [stories] be optimized for parallel work of backend and frontend?"
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

**Create the PRD content following the [example PRD structure](/.windsurf/samples/example-prd.md):**

1. **High-level PRD description:**
   - Use sentence case for level-1 headers
   - Stay at a high level—no implementation details or code examples
   - Use correct domain terminology: multi-tenant, self-contained system, shared kernel, tenant, user, etc.
   - Specify which self-contained system(s) are in scope
   - Avoid repetition

2. **[Stories] section** structured based on wizard answers:

   **Examples based on common patterns:**

   **Example 1 - Parallel optimization (small [feature]):**
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

   **Example 4 - Backend-only [feature]:**
   - Backend implementation (API endpoints, commands, queries, migrations, tests)

   **Example 5 - Large [feature] with multiple cycles:**
   - Frontend core UI with mocks
   - Backend core functionality
   - Integration frontend with backend
   - Frontend advanced features with mocks
   - Backend advanced functionality
   - Integration frontend with backend
   - E2E tests (if E2E tests selected)

   **Note:** These are examples only. Adapt the [story] structure to match the actual [feature] requirements, scope, and user answers.

3. **[Story] guidelines:**
   - Each [story] should be a logical grouping (e.g., "all backend", "all frontend", "all e2e tests")
   - Keep [stories] small enough to review effectively
   - Write a clear paragraph describing what each [story] delivers
   - Each [task] = one commit = one vertical slice
   - All [tasks] in a [story] work together to deliver a coherent [feature]
   - **List [stories] in implementation order** (the order they should be implemented)
   - E2E tests should typically be the final [story]
   - **Important:** When using MCP-based `[PRODUCT_MANAGEMENT_TOOL]`, create [stories] in the same order they appear in the PRD—this defines the implementation sequence

Show the complete PRD to the user - display the full content including all [stories] with their descriptions.

**Ask for approval:** "Does this PRD look good?" (Yes/No)
- If No: Ask what to change, update the PRD content, show again, repeat approval
- If Yes: Continue to Step 6

### Step 6: Confirm PRD name and details

Extract PRD name from feature description, remove imperative verbs ("Create", "Add", "Implement"), convert to sentence case:
- Examples: "Redesign user interface" → "User interface redesign", "Implement SSO Authentication" → "SSO authentication"

Ask "PRD name: '[name]' - correct?" (Yes/Custom)

### Step 7: Create [feature] in [PRODUCT_MANAGEMENT_TOOL]

Follow your [PRODUCT_MANAGEMENT_TOOL]-specific guide at `/.claude/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand how to create items based on the PRD.

Create:
- [feature] with name=[confirmed feature name], assign to "me"
- [story] for each [story] with title=[story title], description=[story description], link to [feature], assign to "me"
- Initialize all items in [Planned] status

**Inform user:** The [feature] has been created from the PRD. Use `/process:create-tasks` to break down each [story] into [tasks].

## Guidelines

✅ DO:
- Follow the exact structure in the example PRD
- Conduct deep research by reading code, consulting rule files, and using MCP tools
- Specify the self-contained system for the [feature]
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
- Create [feature] or [stories] in `[PRODUCT_MANAGEMENT_TOOL]` before getting PRD approval in Step 5
- Rename the file—must be `prd.md`
- Save questions in the PRD file
- Create [stories] that split tests, implementation, and migrations across separate [stories]
- Ask the user clarifying questions before Step 4

**SERIOUSLY:**
Do the research. Read code and rule files. Ask comprehensive questions. Create excellent PRDs. No shortcuts.