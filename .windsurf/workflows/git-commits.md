---
description: Workflow for writing effective git commit messages
---

# Git Commit Workflow

Follow these steps to commit changes:

1. Inspect changes:
   - Review staged changes: `git --no-pager diff --staged`
   - View unstaged changes: `git --no-pager diff`
   - For a combined view: `git --no-pager diff HEAD`

2. Follow these format rules for commit messages:
   - Use imperative form (e.g., "Add feature" not "Added feature" or "Adds feature")
   - Start with a capital letter (sentence case)
   - Do not end the message with punctuation
   - Keep the message to a single line
   - Describe what is changed and the motivation, but keep it concise

3. Build and test the codebase:
   - If changes have been made to backend `*.cs`, run:
      ```bash
      [CLI_ALIAS] test --backend
      ```
   - If changes have been made to frontend `*.ts`, run:
      ```bash
      [CLI_ALIAS] check --frontend
      ```
   - If there are errors, they must be fixed before the commit can be made.

4. Format the codebase:
   - If changes have been made to backend `*.cs`, run:
      ```bash
      [CLI_ALIAS] format --backend
      ```
   - If changes have been made to frontend `*.ts`, run:
      ```bash
      [CLI_ALIAS] format --frontend
      ```

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

# Step 3: Add all changes if needed
git add -A

# Step 4: Commit with a proper message
git commit -m "Add user profile image upload functionality"

# ❌ DON'T: Commit without reviewing changes
git add .
git commit -m "Fix stuff"
```
