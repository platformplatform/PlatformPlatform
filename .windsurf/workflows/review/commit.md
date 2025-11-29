---
description: Commit changes with proper validation and message format
auto_execution_mode: 3
---

# Commit Changes Workflow

Changes description: $ARGUMENTS

Use the description provided in the arguments above (if any) to understand what changes were made. This workflow will help you commit changes to the repository with proper validation and message format.

## Workflow for Reviewers

**Note**: This workflow is for reviewers who have already validated changes through their review process.

### Critical File Scope Verification

**IMPORTANT**: This verification only matters when both backend and frontend engineers are working on the same branch in parallel. If you're the only engineer on the branch, you can skip this check.

**Before committing**, verify you are only committing files within your scope:

**Backend reviewers** should ONLY commit changes in:
- Backend folders: `Api/`, `Core/`, `Workers/`, `Tests/` (C# code)
- Backend-specific files: `.cs`, `.csproj`, `.sln`, database migrations
- Root configuration files that affect backend: `application/global.json`, `application/dotnet-tools.json`, `.editorconfig`, etc.

**Frontend reviewers** should ONLY commit changes in:
- Frontend folders: `WebApp/` (TypeScript/React code)
- Frontend-specific files: `.ts`, `.tsx`, `.css`, `.po`, `.pot`, frontend configs
- Root configuration files that affect frontend: `biome.json`, `package.json`, `tsconfig.json`, etc.

**Special cases**:
- `application/{self-contained-system}/WebApp/shared/lib/api/*.Api.json` belongs to **backend** (auto-generated from API contracts)
- Shared documentation (`README.md`, etc.) belongs to whoever made the functional change being documented
- When in doubt, use your judgment based on what the changes actually affect

**REJECT the commit immediately if**:
- Backend reviewer finds changes in `WebApp/` folders (except `*.Api.json`)
- Frontend reviewer finds changes in `Api/`, `Core/`, `Workers/`, or `Tests/` folders
- Frontend reviewer finds `*.Api.json` files in staged changes

Use `git status --porcelain` to verify file scope before proceeding.

### Commit Message Format

1. Create a proper commit message following these format rules:
   - Use imperative form (e.g., "Add feature" not "Added feature" or "Adds feature")
   - Start with a capital letter (sentence case)
   - Do not end the message with punctuation
   - Keep the message to a single line
   - Describe what is changed and the motivation, but keep it concise

2. Stage files individually and commit:
   ```bash
   git add <file1> <file2> ...
   git commit -m "Your commit message here"
   ```

   🚨 **NEVER use `git add -A` or `git add .`** - always stage files explicitly to avoid committing unintended changes.

## Examples

### Example 1 - Commit Message Format

```
# ✅ DO: Use imperative form, start with capital letter, no punctuation
Enable auto-configuration for data protection in Azure Container Apps
Update all UI texts to consistently use sentence case instead of title case
Upgrade Entity Framework from 8.x to 9.x
Add spacing between top menu buttons to prevent border cropping
Update Azure location codes to use country-specific identifiers
Make logout redirect consistent across all pages

# ❌ DON'T: Use past tense, lowercase, punctuation, or prefixes
Upgraded Entity Framework from 8.x to 9.x
Add spacing between top menu buttons to prevent border cropping.
Making redirect consistent across all pages
update Azure location codes to use country-specific identifiers
feat: enable auto-configuration for data protection in Azure Container Apps
```

### Example 2 - Commit Workflow

```bash
# ✅ DO: Review changes before committing
# Step 1: Review staged changes
git --no-pager diff --staged

# Step 2: Check if there are unstaged changes you want to include
git --no-pager diff

# Step 3: Add files explicitly
git add path/to/file1.cs path/to/file2.ts

# Step 4: Commit with a proper message
git commit -m "Add user profile image upload functionality"

# ❌ DON'T: Use git add -A or git add . or commit without reviewing changes
git add -A
git commit -m "Fix stuff"
```
