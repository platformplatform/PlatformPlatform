# Git Commit Messages

When writing commit messages, follow these rules to maintain consistency across the codebase:

## Inspect changes

1. Review staged changes that will be included in the commit:

    `git --no-pager diff --staged`

2. If there are both staged and unstaged changes, ask if unstaged changes should be included:
   - To view unstaged changes: `git --no-pager diff`
   - To include all changes: `git add -A`
   - To keep only staged changes: proceed with commit

3. For a combined view of all changes (staged and unstaged):

    `git --no-pager diff HEAD`

## Format Rules

1. Use imperative form (e.g., "Add feature" not "Added feature" or "Adds feature")
2. Start with a capital letter (sentence case)
3. Do not end the message with punctuation
4. Keep the message to a single line
5. Describe what is changed and the motivation but keep it concise

## Examples

Good commit messages:
```
Enable auto-configuration for data protection in Azure Container Apps
Update all UI texts to consistently use sentence case instead of title case
Upgrade Entity Framework from 8.x to 9.x
Add spacing between top menu buttons to prevent border cropping
Update Azure location codes to use country-specific identifiers
Make logout redirect consistent across all pages
```

Bad commit messages:
```
Upgraded Entity Framework from 8.x to 9.x
Add spacing between top menu buttons to prevent border cropping.
Making redirect consistent across all pages
update Azure location codes to use country-specific identifiers
feat: enable auto-configuration for data protection in Azure Container Apps
```
