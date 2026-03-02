---
description: Workflow for writing pull request titles and descriptions
---

# Prepare Pull Request Workflow

Use this workflow to create pull request titles and descriptions.

## Workflow

1. Before creating a pull request, gather context by inspecting the changes:
   - Get the full list of commits: `git --no-pager log --format=%s --reverse $(git merge-base HEAD main)..HEAD`
   - View the full diff: `git --no-pager diff main`
   - If needed, examine specific files for more context

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

5. Only for pull requests to the "PlatformPlatform Upstream repository" add a `### Downstream projects` section if needed:
   - Downstream projects integrate all pull requests from the PlatformPlatform Upstream repository into their own downstream repository, where the product is built in the [Main](/application/main) self-contained system
   - If the file [application/PlatformPlatform.slnx](/application/PlatformPlatform.slnx) exists, this is the "PlatformPlatform Upstream repository". In downstream projects, the Visual Studio solution file has been renamed
   - **Important**: Since downstream projects build in [Main](/application/main), all changes already made to `main/` files in the upstream PR arrive automatically when downstream merges. Never tell downstream to redo, verify, or re-apply changes that are already in the upstream `main/` code. The downstream section should only describe genuinely new work that downstream must do in their own custom code
   - Include this section only when changes require modifications in downstream custom code beyond what the upstream merge provides
   - A sign that this is needed is when:
     - Breaking changes have been made to shared components (typically in `application/shared-kernel` or `application/shared-webapp`) that introduce new required props, renamed APIs, or removed features that downstream custom code may use
     - New conventions or patterns are introduced (e.g., a required prop on a shared component) that downstream must apply to their own custom usages
     - Changes to workflow files [main.yml](/.github/workflows/main.yml), [account.yml](/.github/workflows/account.yml), and [back-office.yml](/.github/workflows/back-office.yml) that need equivalent changes in downstream workflow files
   - Use direct, specific language when addressing what needs to be done, e.g., "Add `trackingTitle` to all `Dialog` usages in custom code:"
   - Avoid phrases like "Downstream projects should" or "Downstream projects must". Use more direct phrasing
   - Use a numbered list for multiple changes, make it clear if multiple changes are required
   - Be very specific about the changes needed:
     - Reference `main/` paths when describing where downstream should make changes
     - Only describe changes that downstream must make themselves, never include steps for changes already in the upstream merge
   - Include exact filenames, code snippets, and Git diffs as needed

6. Build, test, format and inspect the codebase using the `execute_command` MCP tool:

   **For backend changes** (`*.cs` files):
   1. Run **build** first: `execute_command(command: "build", backend: true)`
   2. Then run **format**, **test**, **inspect** in parallel (or sequentially if parallel not supported):
      - `execute_command(command: "format", backend: true)`
      - `execute_command(command: "test", backend: true)`
      - `execute_command(command: "inspect", backend: true)`

   **For frontend changes** (`*.ts`, `*.tsx` files):
   1. Run **build** first: `execute_command(command: "build", frontend: true)`
   2. Then run **format** and **inspect** in parallel (or sequentially if parallel not supported):
      - `execute_command(command: "format", frontend: true)`
      - `execute_command(command: "inspect", frontend: true)`

   If there are errors, they must be fixed before the pull request can be created.

7. Save the generated pull request (title and description) to `.workspace/pull-request.md`:
   - Create the file with the pull request title as a level 1 heading, followed by the full description
   - Display the pull request content in the AI editor as normal
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

### Downstream projects

1. Update `main/Api/Program.cs` to use the shared data protection keys. # ✅ DO: use `main/` paths since downstream projects build in main

# ✅ DO: Use Git diff syntax to show what should be changed
   '''diff
   - // No shared data protection configuration
   + // Configure shared data protection keys
   + builder.Services
   +     .AddDataProtection()
   +     .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(".platformplatform", "dataprotection-keys")));
   '''

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

### Example 3 - Output with File Link

After generating the pull request, end the output with a clickable link:

```
Saved to: /Users/thomasjespersen/Developer/PlatformPlatform/.workspace/pull-request.md
```
