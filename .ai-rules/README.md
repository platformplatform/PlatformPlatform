# AI Rules

This directory contains specific guidance for AI assistants to follow when working with this codebase, but also serve as a great reference for developers.

## AI Assistant Instructions

When responding start by reading the relevant instructions from these areas.

- [Backend Rules](/.ai-rules/backend/README.md) 
- [Frontend Rules](/.ai-rules/frontend/README.md)
- [Infrastructure Rules](/.ai-rules/infrastructure/README.md)
- [Developer CLI Rules](/.ai-rules/developer-cli/README.md)

It is **EXTREMELY important that you follow the instructions in the rule files very carefully**.

When making changes to both the frontend and backend, build the backend by running `dotnet build` in the [application](/application) directory to generate the Open API JSON contract. Then run `npm run build` from the [application](/application) directory to trigger `openapi-typescript` to generate the API contract used by the frontend.

When we learn new things that deviate from the existing rules, suggest making changes to the rules files or creating new rules files. When creating new rules files, always make sure to add them to the relevant README.md file.

## Project Structure

This is a mono repository with multiple self-contained systems (SCS), each being a small monolith. All SCSs follow the same structure. Use this overview to gain an understanding of the codebase structure.

- [.github](/.github): GitHub workflows and other GitHub artifacts.
- [application](/application): Contains application code.
  - [account-management](/application/account-management): A SCS for tenant and user management.
    - [WebApp](/application/account-management/WebApp): A React, TypeScript, SPA.
    - [Api](/application/account-management/Api): .NET 9 minimal API.
    - [Core](/application/account-management/Core): .NET 9 Vertical Sliced Architecture.
    - [Workers](/application/account-management/Workers): A .NET Console job.
    - [Tests](/application/account-management/Tests): xUnit tests for backend.
  - [back-office](/application/back-office): An empty SCS, that will be used to create tools for Support and System Admins.
    - Follows exactly the same structure as Account Management with WebApp, Api, Core, Workers, Tests.
  - [AppHost](/application/AppHost): .NET Aspire project for orchestrating SCSs and Docker containers (only use locally).
  - [AppGateway](/application/AppGateway): Main entry point using .NET YARP as reverse proxy for all SCSs.
  - [shared-kernel](/application/shared-kernel): Reusable .NET Backend shared by all SCSs.
  - [shared-webapp](/application/shared-webapp): Reusable frontend shared by all SCSs.
- [cloud-infrastructure](/cloud-infrastructure): Bash and Azure Bicep scripts (IaC).
- [developer-cli](/developer-cli): A .NET CLI tool for automating common developer tasks.
