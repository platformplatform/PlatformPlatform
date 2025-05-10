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
6. After each change make sure you follow the rules in [Backend Rules](/.windsurf/rules/backend/backend.md) or [Frontend Rules](/.windsurf/rules/frontend/frontend.md), on how to correctly use the custom CLI tool for building, testing, and formatting the code.
   - Failure to use the custom CLI tool after each change is the second most common reason for making unacceptable changes.

## Overview of all guidelines rule files

### Backend Development
- [Backend Rules](/.windsurf/rules/backend/backend.md) - Core guidelines for C# and .NET development:
  - [API Endpoints](/.windsurf/rules/backend/api-endpoints.md) - Guidelines for minimal API endpoints.  
  - [Commands](/.windsurf/rules/backend/commands.md) - Implementation of state-changing operations using CQRS commands.
  - [Database Migrations](/.windsurf/rules/backend/database-migrations.md) - Guidelines for database migrations.
  - [Domain Modeling](/.windsurf/rules/backend/domain-modeling.md) - Implementation of DDD aggregates, entities, and value objects.
  - [External Integrations](/.windsurf/rules/backend/external-integrations.md) - Implementation of integrations to external services.
  - [Queries](/.windsurf/rules/backend/queries.md) - Implementation of data retrieval operations using CQRS queries.
  - [Repositories](/.windsurf/rules/backend/repositories.md) - Persistence abstractions for aggregates.
  - [Strongly Typed IDs](/.windsurf/rules/backend/strongly-typed-ids.md) - Type-safe DDD identifiers for domain entities.
  - [Telemetry Events](/.windsurf/rules/backend/telemetry-events.md) - Standardized observability event patterns.
  - [Unit and Integration Tests](/.windsurf/rules/backend/unit-and-integration-tests.md) - Test suite patterns for commands, queries, and domain logic.

### Frontend Development
- [Frontend Rules](/.windsurf/rules/frontend/frontend.md) - Core guidelines for React/TypeScript:
  - [Form with Validation](/.windsurf/rules/frontend/form-with-validation.md) - Forms with validation and API integration using TanStack Query.
  - [Modal Dialog](/.windsurf/rules/frontend/modal-dialog.md) - Modal dialog implementation patterns.
  - [React Aria Components](/.windsurf/rules/frontend/react-aria-components.md) - Usage of shared component library.
  - [TanStack Query API Integration](/.windsurf/rules/frontend/tanstack-query-api-integration.md) - Data fetching and mutation patterns.
  - [Translations](/.windsurf/rules/frontend/translations.md) - Internationalization implementation for UI text.

### Azure Infrastructure
- [Infrastructure Rules](/.windsurf/rules/infrastructure/infrastructure.md) - Cloud infrastructure guidelines.

### Developer CLI
- [Developer CLI Rules](/.windsurf/rules/developer-cli/developer-cli.md) - Guidelines for extending the custom Developer CLI.

### Other Rules
- [AI Rules](/.windsurf/rules/other/ai-rules.md) - Guidelines for creating and updating AI rules.
- [Git Commit Rules](/.windsurf/rules/other/git-commits.md) - Guidelines for creating commit messages.
- [Pull Request Rules](/.windsurf/rules/other/pull-request.md) - Guidelines for creating pull requests.

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
