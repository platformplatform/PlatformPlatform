---
trigger: always_on
description: Main entry point for AI-based development and developer reference
---

# Main Entry Point

This is the main entry point for AI-based development when working with this codebase, but also serves as a great reference for developers.

Always follow these rule files very carefully, as they have been crafted to ensure consistency and high-quality code.

## High-Level Problem Solving Strategy

1. Understand the problem deeply. Carefully read the instructions and think critically about what is required.
2. Investigate the codebase. Explore relevant files, search for key functions, and gather context.
3. Develop a clear, step-by-step plan. Break down the fix into manageable, incremental steps.
4. Before each code change, always consult the relevant rule files, and follow the rules very carefully.
   - Failure to follow the rules is the main reason for making unacceptable changes.
5. Iterate until you are extremely confident the fix is complete.
   - When changing code, do not add comments about what you changed.
6. After each change, make sure you follow the rules in [Backend Rules](/.windsurf/rules/backend/backend.md) or [Frontend Rules](/.windsurf/rules/frontend/frontend.md) on how to correctly use the [CLI_ALIAS] CLI tool for building, testing, and formatting the code.
   - Failure to use the [CLI_ALIAS] CLI tool after each change is the second most common reason for making unacceptable changes.
   - Always use the [CLI_ALIAS] CLI commands as described in [Tools](/.windsurf/rules/tools.md) for building, testing, formatting, and inspecting code.

## Rules for implementing changes

Always consult the relevant rule files before each code change.

Please note that I often correct or even revert code you generated. If you notice that, take special care not to revert my changes.

Commit messages should be in imperative form, start with a capital letter, avoid ending punctuation, be a single line, and concisely describe changes and motivation.

Be very careful with comments, and add them only very sparingly. Never add comments about changes made (these belong in pull requests).

When making changes, always take speial care not to change parts of the code that are not in scope.

## Project Structure

This is a mono repository with multiple self-contained systems (SCS), each being a small monolith. All SCSs follow the same structure.

- [.github](/.github): GitHub workflows and other GitHub artifacts.
- [application](/application): Contains application code:
  - [account-management](/application/account-management): An SCS for tenant and user management:
    - [WebApp](/application/account-management/WebApp): A React, TypeScript SPA.
    - [Api](/application/account-management/Api): .NET 10 minimal API.
    - [Core](/application/account-management/Core): .NET 10 Vertical Sliced Architecture.
    - [Workers](/application/account-management/Workers): A .NET Console job.
    - [Tests](/application/account-management/Tests): xUnit tests for backend.
  - [back-office](/application/back-office): An empty SCS that will be used to create tools for Support and System Admins:
    - [WebApp](/application/back-office/WebApp): A React, TypeScript SPA.
    - [Api](/application/back-office/Api): .NET 10 minimal API.
    - [Core](/application/back-office/Core): .NET 10 Vertical Sliced Architecture.
    - [Workers](/application/back-office/Workers): A .NET Console job.
    - [Tests](/application/back-office/Tests): xUnit tests for backend.
  - [AppHost](/application/AppHost): Aspire project for orchestrating SCSs and Docker containers. Never run directly—typically running in watch mode.
  - [AppGateway](/application/AppGateway): Main entry point using YARP as reverse proxy for all SCSs.
  - [shared-kernel](/application/shared-kernel): Reusable .NET backend shared by all SCSs.
  - [shared-webapp](/application/shared-webapp): Reusable frontend shared by all SCSs.
- [cloud-infrastructure](/cloud-infrastructure): Bash and Azure Bicep scripts (IaC).
- [developer-cli](/developer-cli): A .NET CLI tool for automating common developer tasks.
