When replying, start by listing the sections in this rules file that you used to guide your response. Just print the headings without any other details.

# Project Overview
PlatformPlatform is an enterprise-grade, multi-tenant foundation for SaaS startups. It employs a self-contained systems (SCS) architecture with micro-frontends using module federation, ensuring scalability and maintainability.

PlatformPlatform is a monorepo containing all application code, infrastructure, tools, libraries, documentation, etc.

```bash
.
├─ .github               # Separate GitHub workflows for deploying Infrastructure and app
├─ application           # Contains the application source code
│  ├─ AppHost            # .NET Aspire project starting app and all dependencies in Docker
│  ├─ AppGateway         # Main entry point for the app using YARP as a reverse proxy 
│  ├─ account-management # Self-contained system with account sign-up, user management, etc.
│  │   ├─ WebApp         # React SPA frontend using TypeScript and React Aria Components
│  │   ├─ Api            # Presentation layer exposing the API to WebApp or other clients
│  │   ├─ Core           # Core business logic, application use cases, and infrastructure
│  │   ├─ Workers        # Background workers for long-running tasks and event processing
│  │   └─ Tests          # Tests for the Api, Core, and Workers
│  ├─ shared-kernel      # Reusable components and default configuration for all systems
│  ├─ shared-webapp      # Reusable and styled React Aria Components that affect all systems 
│  ├─ [saas-scs]         # [Your SCS] Create your SaaS product as a self-contained system
│  └─ back-office        # A self-contained system for operations and support (empty for now)
├─ cloud-infrastructure  # Contains Bash and Bicep scripts (IaC) for Azure resources
│  ├─ cluster            # Scale units like production-west-eu, production-east-us, etc.
│  ├─ environment        # Shared resources like App Insights, Container Registry, etc.
│  └─ modules            # Reusable Bicep modules like Container App, SQL Server, etc.
└─ development-cli       # A .NET CLI tool for automating common developer tasks
```

Instead of making changes in the root of the monorepo, make code changes in the relevant SCS or in the /application folder.

# Backend Guidelines
- Utilize .NET 9 features, including Aspire for project orchestration.
- When implementing new code, closely examine the coding conventions used in this project:
  - Use C# top-level namespaces.
  - Minimal API endpoints are always only one line with a call to mediator.Send().
  - CQRS Requests, Commands, Validators, and Handlers are in one shared file, ensuring one file per feature, without a folder.
- Adhere to the vertical slice architecture pattern:
  - Create new features in the /Features subfolder in the SCS .Core project
  - Group related functionality (command/query, validator, handler) in a single file
  - Follow naming convention: [Feature][Command/Query].cs
  - Place shared domain logic in /Features/Shared but make sure that each class does one thing only
  - Keep feature files focused and cohesive
  - When creating integrations with external dependencies, create a client in /Client/[ServiceName]/ServiceClient.cs
- The @application/shared-kernel/SharedKernel contains reusable components:
  - Use the RepositoryBase for creating repositoires, but only add the methods to the IRepository interfase that is needed (e.g. don't add Update and Delete methods for a readonly repository)
  - Inherit from AggregateRoot for domain entities
  - Avoid properties pointing to other aggregates to prevent eager or lazy loading; use explicit code for multiple database requests instead of creating an Order.Customer property in an e-commerce app
  - Utilize Result<T> for operation results
  - Only use exceptions when something is truly exceptional (system down or a bug)
  - Apply FluentValidation for input validation
- Write unit tests with xUnit and Fluent Assertions:
  - Follow naming: [Method_WhenX_ShouldY]
  - Use NSubstitute for mocking
  - Test command/query handlers independently
  - Prefer testing API endpoints over unit tests, focusing on behavior rather than implementation.

# Frontend Guidelines
- Develop using React with TypeScript and React Aria Components:
  - Use onPress instead of onClick for button interactions
  - Implement proper keyboard navigation and focus management
  - Follow React Aria Components patterns for accessibility
  - Use compound components pattern when building complex UI
- Style components using:
  - Tailwind variants (tv) for dynamic styling
  - React Aria's built-in style system
  - CSS modules for component-specific styles
- State management:
  - Use React Query for server state
  - Implement React Context for shared UI state
  - Prefer local state when possible
- API integration:
  - Use PlatformApiClient for all API calls:
    ```typescript
    await api.get("/api/account-management/users/{id}", {
      params: { path: { id: userId } }
    });
    ```
  - Handle loading and error states consistently
- Component structure:
  - Place components in feature-specific folders
  - Use index.ts files for public exports
  - Keep components focused and composable
- Dependencies:
  - Use specific versions (no ^ or ~)
  - Prefer React Aria Components over custom implementations
  - Minimize external dependencies

# Shared-Web Project
- The Shared-Web project contains components intended for reuse across all SCSs.
- Modify Shared-Web only when changes should apply universally.

# General Practices
- AVOID making changes to code that are not related to the change we are doing. E.g. don't remove comments or types.
- Assume all code is working as intended, as everything has been carefully crafted.
- Commit messages should be in imperative form, sentence case, starting with a verb, and have NO trailing dot.
- Use descriptive variables names with auxiliary verbs (e.g. isLoading, hasError)

# AI Editor Instructions
- Always reference these sections when generating code
- Maintain consistency with existing patterns
- Prioritize accessibility and type safety
- Follow vertical slice architecture for backend
- Use React Aria Components patterns for frontend
- Generate comprehensive test coverage
- Include proper error handling
- Add meaningful comments explaining complex logic
- Ensure proper validation and security measures
