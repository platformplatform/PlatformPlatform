---
trigger: always_on
description: Main entry point for AI-based development and developer reference
---

# Main Entry Point

This is the main entry point for AI-based development when working with this codebase, but also serves as a great reference for developers.

Always follow these rule files very carefully, as they have been crafted to ensure consistency and high-quality code.

## Development Approach

When working on tasks, follow any specific workflow instructions provided for your role. If no specific workflow is provided:

1. Understand the problem and requirements clearly.
2. Consult the relevant rule files before implementation.
3. Develop a clear implementation plan.
4. Follow established patterns and conventions.
5. Use MCP tools for building, testing, and formatting.
   - Use the **build**, **test**, **format**, **inspect**, **watch**, and **e2e** MCP tools
   - **Important**: The **watch** MCP tool restarts the application server and runs database migrations. The server should typically already be running at https://localhost:9000. Use watch if you suspect the database needs to be migrated, if you need to restart the server for any reason, or if it's not running.
   - BrowserMCP available for frontend troubleshooting at https://localhost:9000
   - See [Tools](/.claude/rules/tools.md) for details

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
    - [Api](/application/account-management/Api): .NET 9 minimal API.
    - [Core](/application/account-management/Core): .NET 9 Vertical Sliced Architecture.
    - [Workers](/application/account-management/Workers): A .NET Console job.
    - [Tests](/application/account-management/Tests): xUnit tests for backend.
  - [back-office](/application/back-office): An empty SCS that will be used to create tools for Support and System Admins:
    - [WebApp](/application/back-office/WebApp): A React, TypeScript SPA.
    - [Api](/application/back-office/Api): .NET 9 minimal API.
    - [Core](/application/back-office/Core): .NET 9 Vertical Sliced Architecture.
    - [Workers](/application/back-office/Workers): A .NET Console job.
    - [Tests](/application/back-office/Tests): xUnit tests for backend.
  - [AppHost](/application/AppHost): .NET Aspire project for orchestrating SCSs and Docker containers. Never run directly—typically running in watch mode.
  - [AppGateway](/application/AppGateway): Main entry point using .NET YARP as reverse proxy for all SCSs.
  - [shared-kernel](/application/shared-kernel): Reusable .NET backend shared by all SCSs.
  - [shared-webapp](/application/shared-webapp): Reusable frontend shared by all SCSs.
- [cloud-infrastructure](/cloud-infrastructure): Bash and Azure Bicep scripts (IaC).
- [developer-cli](/developer-cli): A .NET CLI tool for automating common developer tasks.