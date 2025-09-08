---
description: Create pull request with proper title and description
argument-hint: Branch name or feature description (optional)
---

# Prepare Pull Request Workflow

Pull request context: $ARGUMENTS

Use the branch name or feature description provided in the arguments above (if any) as context for the pull request. This workflow will help you create pull request titles and descriptions:

## Workflow

1. Before creating a pull request, gather context by inspecting the changes:
   - Get the full list of commits: `git --no-pager log --format=%s --reverse $(git merge-base HEAD main)..HEAD`.
   - View the full diff: `git --no-pager diff main`.
   - If needed, examine specific files for more context.

2. Create a pull request title that is:
   - In imperative form ("Fix", "Add", "Upgrade", "Refactor", etc.).
   - Written as a sentence (not in Title Case).
   - Without a period at the end.

3. Write a clear description that:
   - Starts with an explanation of what the change does and why it was made.
   - Focuses on the functional impact of the change without overselling.
   - Do not use the term "Pull Request", "PR", or any abbreviation of it.
   - Do not use personal pronouns such as "we", "our", "he", "she".
   - DO NOT end the pull request description summarizing the benefits of the changes.
     - Adding a such a summary at the end is the most common reason pull request descriptions are not accepted.

4. Always use the official pull request template [.github/PULL_REQUEST_TEMPLATE.md](/.github/PULL_REQUEST_TEMPLATE.md) which includes:
   - **Summary & Motivation**: Start with the most important change, use bullet points for multiple changes, and mention minor fixes last.
   - **Checklist**: Do not change the checklist from .github/PULL_REQUEST_TEMPLATE.md, and do not [x] the list, as this should be a manual task.

5. Only for pull requests to the "PlatformPlatform Upstream repository" add a `### Downstream projects` section if needed:
   - Downstream projects integrate all pull requests from the PlatformPlatform Upstream repository into their own downstream repository, which typically has one extra self-contained system.
   - If the file [application/PlatformPlatform.slnx](/application/PlatformPlatform.slnx) exists, this is the "PlatformPlatform Upstream repository". In downstream projects, the Visual Studio solution file has been renamed.
   - Include this section only when changes require modifications in downstream projects.
   - A sign that this is needed is when:
     - Similar changes have been made across the two self-contained systems [Account Management](/application/account-management) and [Back Office](/application/back-office).
     - Similar changes have been made to workflow files [account-management.yml](/.github/workflows/account-management.yml) and [back-office.yml](/.github/workflows/back-office.yml).
     - Breaking changes have been made to shared components (typically in `application/shared-kernel` or `application/shared-webapp`) that might require changes in downstream projects.
     - Updates to NuGet or npm packages required changes to [Account Management](/application/account-management) and [Back Office](/application/back-office) that also needed to be applied to other self-contained systems.
   - Use direct, specific language when addressing what needs to be done, e.g., "Please update your custom configuration to match these changes:".
   - Avoid phrases like "Downstream projects should" or "Downstream projects must" - use more direct phrasing.
   - Use a numbered list for multiple changes, make it clear if multiple changes are required.
   - Be very specific about the changes needed.
     - Often any changes to [Back Office](/application/back-office) are 1:1 with the changes that need to be applied to downstream self-contained systems.
   - Include exact filenames, code snippets, and Git diffs as needed.
   - Use the placeholder term `your-self-contained-system` for downstream implementations.

6. Build, test, format and inspect the codebase:
   - If changes have been made to backend `*.cs` but only to one self-contained system, run:
      ```bash
      [CLI_ALIAS] check --backend --solution-name SelfContainedSystem.slnf
      ```
   - If backend changes have been made to `*.cs` in multiple self-contained systems or the Shared Kernel, run:
      ```bash
      [CLI_ALIAS] check --backend
      ```
   - If changes have been made to frontend `*.ts`, run:
      ```bash
      [CLI_ALIAS] check --frontend
      ```
   - If there are errors, they must be fixed before the pull request can be created.

## Examples

### Example 1 - Pull request title

```
# ✅ DO: Use imperative form, start with capital letter, no punctuation
Add user profile image upload functionality
Fix data protection key sharing between SCSs
Update dependency versions to latest stable releases

# ❌ DON'T: Use past tense, end with period, or use title case
Added User Profile Image Upload Functionality.
Fixed a bug
Updating dependencies
PR: Implement new feature
```

### Example 2 - Pull request description

```markdown
### Summary & Motivation

Add data protection key sharing between self-contained systems to fix antiforgery token validation failures. Previously, each self-contained system had isolated encryption keys, causing tokens generated in one system to be invalid in another.

- Configure a common application name ("PlatformPlatform") for all SCSs.
- Store keys in a user-accessible directory on disk.

# ✅ DO: Correctly NOT adding a short summary of the benefits here!!!

### Downstream projects

1. Update `your-self-contained-system/Api/Program.cs` to use the shared data protection keys. # ✅ DO: use `your-self-contained-system` to reference downstream system

# ✅ DO: Use Git diff syntax to show what should be changed
   ```diff
- // No shared data protection configuration
+ // Configure shared data protection keys
+ builder.Services
+     .AddDataProtection()
+     .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(".platformplatform", "dataprotection-keys")));
```

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

### Downstream projects # ❌ DON'T: Include steps for downstream projects when all changes are done in PlatformPlatform

Update SharedKernel with new feature.

### Checklist  # ❌ DON'T: Use made up checklist

- [x] Code follows the style guidelines of the project
- [x] Changes have been tested locally
```