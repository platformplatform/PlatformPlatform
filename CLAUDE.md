# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Main Entry Point

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
6. After each change, make sure you follow the rules in Backend Rules or Frontend Rules on how to correctly use the [CLI_ALIAS] CLI tool for building, testing, and formatting the code.
   - Failure to use the [CLI_ALIAS] CLI tool after each change is the second most common reason for making unacceptable changes.
   - Always use the [CLI_ALIAS] CLI commands as described in Tools for building, testing, formatting, and inspecting code.

## Rules for implementing changes

Always consult the relevant rule files before each code change.

Please note that I often correct or even revert code you generated. If you notice that, take special care not to revert my changes.

Commit messages should be in imperative form, start with a capital letter, avoid ending punctuation, be a single line, and concisely describe changes and motivation.

## CLI Alias Configuration

The `[CLI_ALIAS]` is configured as `pp` (PlatformPlatform CLI). This Developer CLI should be used for all build, test, and format operations instead of direct commands.

**IMPORTANT:** Never fall back to using direct commands like `npm run format`, `dotnet test`, `npx playwright test`, `npm test`, etc. Always use the Developer CLI with the appropriate alias.

## Build Commands

Use these commands continuously when you are working on the codebase.

```bash
# Build both backend and frontend
pp build

# Build only backend
pp build --backend

# Build specific backend solution
pp build --backend --solution-name <solution-name>

# Build only frontend
pp build --frontend
```

## Test Commands

After you have completed a backend task and want to ensure that it works as expected, run the test commands.

```bash
# Run all tests
pp test

# Run tests for specific solution
pp test --solution-name <solution-name>
```

## End-to-End Test Commands

```bash
# Run all end-to-end tests except slow tests
pp e2e

# Run end-to-end tests for specific solution
pp e2e --self-contained-system <self-contained-system-name>

# Run end-to-end tests for specific browser
pp e2e --browser <browser-name>

# Run end-to-end tests for specific test
pp e2e --grep <test-name>

# Run end-to-end tests for specific test and browser
pp e2e --grep "@tag"

# Include slow tests (excluded by default)
pp e2e --include-slow
```

## Format Commands

Run these commands before you commit your changes.

```bash
# Format both backend and frontend (run this before commit)
pp format

# Format only backend (run this before commit)
pp format --backend

# Format specific backend solution (run this before commit)
pp format --backend --solution-name <solution-name>

# Format only frontend (run this before commit)
pp format --frontend
```
## Rules for implementing changes

Always consult the relevant rule files before each code change.

*General Rules*:
- [Tools](/.windsurf/rules/tools.md) - Rules for how to use Developer CLI tools to build, test, and format code correctly over using direct commands like `npm run format` or `dotnet test`.

*Backend*:
- [Backend](/.windsurf/rules/backend/backend.md) - Core rules for C# development and tooling
- [API Endpoints](/.windsurf/rules/backend/api-endpoints.md) - Rules for ASP.NET minimal API endpoints
- [Commands](/.windsurf/rules/backend/commands.md) - Rules for implementing CQRS commands, validation, handlers, and structure
- [Database Migrations](/.windsurf/rules/backend/database-migrations.md) - Rules for creating database migrations
- [Domain Modeling](/.windsurf/rules/backend/domain-modeling.md) - Rules for creating DDD aggregates, entities, value objects, and Entity Framework configuration
- [External Integrations](/.windsurf/rules/backend/external-integrations.md) - Rules for creating external integration services
- [Queries](/.windsurf/rules/backend/queries.md) - Rules for CQRS queries, including structure, validation, response types, and mapping
- [Repositories](/.windsurf/rules/backend/repositories.md) - Rules for DDD repositories, including tenant scoping, interface conventions, and use of Entity Framework
- [Strongly Typed IDs](/.windsurf/rules/backend/strongly-typed-ids.md) - Rules for creating strongly typed IDs for DDD aggregates and entities
- [Telemetry Events](/.windsurf/rules/backend/telemetry-events.md) - Rules for telemetry events including important rules of where to create events, naming, and what properties to track
- [API Tests](/.windsurf/rules/backend/api-tests.md) - Rules for writing backend API tests

*Frontend*:
- [Frontend](/.windsurf/rules/frontend/frontend.md) - Core rules for frontend TypeScript and React development
  - [Form with Validation](/.windsurf/rules/frontend/form-with-validation.md) - Rules for forms with validation using React Aria Components
  - [Modal Dialog](/.windsurf/rules/frontend/modal-dialog.md) - Rules for modal dialogs using React Aria Components
  - [React Aria Components](/.windsurf/rules/frontend/react-aria-components.md) - Rules for using React Aria Components
  - [TanStack Query API Integration](/.windsurf/rules/frontend/tanstack-query-api-integration.md) - Rules for using TanStack Query with backend APIs
  - [Translations](/.windsurf/rules/frontend/translations.md) - Rules for translations and internationalization

*Infrastructure*:
- [Infrastructure](/.windsurf/rules/infrastructure/infrastructure.md) - Rules for cloud infrastructure and deployment

*Developer CLI*:
- [Developer CLI](/.windsurf/rules/developer-cli/developer-cli.md) - Rules for implementing Developer CLI commands

*Workflows*:
- [AI Rules Workflow](/.windsurf/workflows/ai-rules.md) - Workflow for creating and maintaining AI rules
- [Code Review Workflow](/.windsurf/workflows/code-review.md) - Workflow for code review of branches, uncommitted changes, or files
- [Git Commits Workflow](/.windsurf/workflows/git-commits.md) - Workflow for writing effective git commit messages
- [Pull Request Workflow](/.windsurf/workflows/pull-request.md) - Workflow for writing pull request titles and descriptions
- [Update Windsurf Rules Workflow](/.windsurf/workflows/update-windsurfrules.md) - Rules for updating the .windsurfrules file which is used by Windsurf's JetBrains Add-in.

*End-to-End Testing*:
- [End-to-End Testing](/.windsurf/rules/e2e/e2e.md) - Rules for end-to-end testing

## Project Structure

This is a mono repository with multiple self-contained systems (SCS), each being a small monolith. All SCSs follow the same structure.

- **application/**: Contains application code:
  - **account-management/**: An SCS for tenant and user management:
    - **WebApp/**: A React, TypeScript SPA.
    - **Api/**: .NET 9 minimal API.
    - **Core/**: .NET 9 Vertical Sliced Architecture.
    - **Workers/**: A .NET Console job.
    - **Tests/**: xUnit tests for backend.
  - **back-office/**: An empty SCS that will be used to create tools for Support and System Admins:
    - **WebApp/**: A React, TypeScript SPA.
    - **Api/**: .NET 9 minimal API.
    - **Core/**: .NET 9 Vertical Sliced Architecture.
    - **Workers/**: A .NET Console job.
    - **Tests/**: xUnit tests for backend.
  - **AppHost/**: .NET Aspire project for orchestrating SCSs and Docker containers. Never run directly—typically running in watch mode.
  - **AppGateway/**: Main entry point using .NET YARP as reverse proxy for all SCSs.
  - **shared-kernel/**: Reusable .NET backend shared by all SCSs.
  - **shared-webapp/**: Reusable frontend shared by all SCSs.
- **cloud-infrastructure/**: Bash and Azure Bicep scripts (IaC).
- **developer-cli/**: A .NET CLI tool for automating common developer tasks.
