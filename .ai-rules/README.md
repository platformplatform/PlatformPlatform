# AI Rules

# Workflow

## High-Level Problem Solving Strategy

1. Understand the problem deeply. Carefully read the instructions and think critically about what is required.
2. Always ask questions if something is unclear before starting to implement.
3. Investigate the codebase. Explore relevant files, search for key functions, and gather context.
4. Develop a clear, step-by-step plan. Break down the fix into manageable, incremental steps.
5. Before each code change, always read the relevant rule files (e.g. read [Commands](/.ai-rules/backend/commands.md) when working with CQRS commands), to make sure you understand the rules and follow the conventions in the codebase. Failure to do this is the main reason for making unacceptable changes.
6. Iterate until you are extremely confident the fix is complete and all tests pass.
7. After each change make sure you follow the rules in [Backend Rules](/.ai-rules/backend/README.md) or [Frontend Rules](/.ai-rules/frontend/README.md), on how to correctly use the custom CLI tool for building, testing, and formatting the code. Never use e.g. npm or dotnet CLI directly.
   - Failure to use the custom CLI tool after each change is the second most common reason for making unacceptable changes.

## AI Rules

When making changes consult these files depending on what part of the system you are changing.

- [Backend Rules](/.ai-rules/backend/README.md) 
- [Frontend Rules](/.ai-rules/frontend/README.md)
- [Infrastructure Rules](/.ai-rules/infrastructure/README.md)
- [Developer CLI Rules](/.ai-rules/developer-cli/README.md)
- Other Rules
  - [Git Commit Rules](/.ai-rules/other/git-commits.md)
  - [Pull Request Rules](/.ai-rules/other/pull-request.md)

## Project Structure

Use the structure below to understand the codebase. This is a mono repository with multiple self-contained systems (SCS), each being a small monolith. All SCSs follow the same structure. Use this overview to gain an understanding of the codebase structure.

- [.github](/.github): GitHub workflows and other GitHub artifacts
- [application](/application): Contains application code
  - [account-management](/application/account-management): A SCS for tenant and user management
    - [WebApp](/application/account-management/WebApp): A React, TypeScript, SPA
    - [Api](/application/account-management/Api): .NET 9 minimal API
    - [Core](/application/account-management/Core): .NET 9 Vertical Sliced Architecture
    - [Workers](/application/account-management/Workers): A .NET Console job
    - [Tests](/application/account-management/Tests): xUnit tests for backend
  - [back-office](/application/back-office): An empty SCS, that will be used to create tools for Support and System Admins
    - [WebApp](/application/back-office/WebApp): A React, TypeScript, SPA
    - [Api](/application/back-office/Api): .NET 9 minimal API
    - [Core](/application/back-office/Core): .NET 9 Vertical Sliced Architecture
    - [Workers](/application/back-office/Workers): A .NET Console job
    - [Tests](/application/back-office/Tests): xUnit tests for backend
  - [AppHost](/application/AppHost): .NET Aspire project for orchestrating SCSs and Docker containers **
  - [AppGateway](/application/AppGateway): Main entry point using .NET YARP as reverse proxy for all SCSs
  - [shared-kernel](/application/shared-kernel): Reusable .NET Backend shared by all SCSs
  - [shared-webapp](/application/shared-webapp): Reusable frontend shared by all SCSs
- [cloud-infrastructure](/cloud-infrastructure): Bash and Azure Bicep scripts (IaC)
- [developer-cli](/developer-cli): A .NET CLI tool for automating common developer tasks

** The AppHost project is the entry point. Do NOT RUN THIS as it's likely already running in watch mode.