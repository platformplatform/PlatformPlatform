---
description: Workflow for writing pull request titles and descriptions
---

# Prepare Pull Request Workflow

Use this workflow to create pull request titles and descriptions.

## Workflow

Team leads: execute this workflow directly. Do not delegate it.

1. Before creating a pull request, gather context by inspecting the changes:
   - Get the full list of commits: `git --no-pager log --format=%s --reverse $(git merge-base HEAD main)..HEAD`
   - View the full diff: `git --no-pager diff main`

2. Create a pull request title that is:
   - In imperative form ("Fix", "Add", "Upgrade", "Refactor", etc.)
   - Written as a sentence (not in Title Case)
   - Without a period at the end

3. Write a clear description that:
   - Starts with an explanation of what the change does and why it was made
   - Focuses on the functional impact of the change without overselling
   - Does not use the term "Pull Request", "PR", or any abbreviation of it
   - Does not use personal pronouns such as "we", "our", "he", "she"
   - Does NOT end with a summary of the benefits—adding such a summary is the most common reason pull request descriptions are rejected

4. Always use the official pull request template [.github/PULL_REQUEST_TEMPLATE.md](/.github/PULL_REQUEST_TEMPLATE.md) which includes:
   - **Summary & Motivation**: Start with the most important change, use bullet points for multiple changes, and mention minor fixes last
   - **Checklist**: Do not change the checklist from .github/PULL_REQUEST_TEMPLATE.md, and do not [x] the list—this should be a manual task

5. Build, test, format and lint the codebase using the MCP tools:

   **For backend changes** (`*.cs` files):
   1. Run **build** first: `build(backend=true)`
   2. Then run **format**, **test**, **lint** in parallel (or sequentially if parallel not supported):
      - `format(backend=true)`
      - `test(backend=true)`
      - `lint(backend=true)`

   **For frontend changes** (`*.ts`, `*.tsx` files):
   1. Run **build** first: `build(frontend=true)`
   2. Then run **format** and **lint** in parallel (or sequentially if parallel not supported):
      - `format(frontend=true)`
      - `lint(frontend=true)`

   If there are errors, they must be fixed before the pull request can be created.

6. Save the pull request title (as a level 1 heading) and description to `.workspace/<branch-name>/pull-request.md`
   - End with a clickable link to the saved file using the full absolute path

## Examples

### Example 1 - Pull Request Title

```
# ✅ DO: Use imperative form, start with capital letter, no punctuation
Add user profile image upload functionality
Fix data protection key sharing between self-contained systems
Update dependency versions to latest stable releases

# ❌ DON'T: Use past tense, end with period, or use title case
Added User Profile Image Upload Functionality.
Fixed a bug
Updating dependencies
PR: Implement new feature
```

### Example 2 - Pull Request Description

```markdown
### Summary & Motivation

Add data protection key sharing between self-contained systems to fix antiforgery token validation failures. Previously, each self-contained system had isolated encryption keys, causing tokens generated in one system to be invalid in another.

- Configure a common application name ("PlatformPlatform") for all self-contained systems
- Store keys in a user-accessible directory on disk

# ✅ Correctly NOT adding a short summary of the benefits here!!!

### Checklist # ✅ DO: Correctly add the check points from PULL_REQUEST_TEMPLATE.md without setting the [x]

- [ ] I have added tests, or done manual regression tests
- [ ] I have updated the documentation, if necessary
```

```markdown
### Summary

# ❌ DON'T: Use "we" and "our" personal pronouns, use past tense, use banned "pull request" term,  use scs acronyms, and add vague descriptions.
In this pull request we fixed a bug causing issues in our scs's.

- We added some configuration.
- Fixed a bug.

These changes make the system more robust and maintainable. # ❌ DON'T: Create short summary statements to finish the description. Skip this line

### Checklist  # ❌ DON'T: Use made up checklist

- [x] Code follows the style guidelines of the project
- [x] Changes have been tested locally
```

### Example 3 - Output with File Link

End with a clickable link to the saved file:

```
Saved to: /absolute/path/to/repo/.workspace/[branch-name]/pull-request.md
```
