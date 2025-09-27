---
description: Workflow for commit changes with proper validation and message format
auto_execution_mode: 1
---

# Commit Changes Workflow

Changes description: $ARGUMENTS

Use the description provided in the arguments above (if any) to understand what changes were made. This workflow will help you commit changes to the repository with proper validation and message format.

## Workflow for Reviewers

**Note**: This workflow is for reviewers who have already validated changes through their review process.

1. Create a proper commit message following these format rules:
   - Use imperative form (e.g., "Add feature" not "Added feature" or "Adds feature").
   - Start with a capital letter (sentence case).
   - Do not end the message with punctuation.
   - Keep the message to a single line.
   - Describe what is changed and the motivation, but keep it concise.

2. Stage all changes and commit:
   ```bash
   git add -A
   git commit -m "Your commit message here"
   ```

## Examples

### Example 1 - Commit message format

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

### Example 2 - Commit workflow

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