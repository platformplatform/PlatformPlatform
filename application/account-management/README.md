# Account Management
Account Management is the core system in PlatformPlatform. Currently, it offers a skeleton of the essential part of any multi-tenant SaaS (Software-as-a-Service) solution, like creating tenants, users, etc. Eventually, it will create a frontend, with login, authentication, license management, feature toggling, and the like. As of now, this is just showcasing how aggregates provide the fundamental building blocks for managing very simple tenant and user data within the system.

Account Management is a self-contained system (a flavor of microservices just bigger - think small monoliths). The idea is that you can eventually reuse the Account Management self-contained system directly and then build your SaaS service as a separate self-contained system, or you can extend this system and build a monolith.

## ASP.NET Minimal API
This project utilizes the lightweight and high-performance ASP.NET Minimal API. It's a modern, simplified hosting and coding model for HTTP APIs, reducing the boilerplate and offering more flexibility.

## Built on a Reusable Shared Kernel
The Account Management system is built on the Shared Kernel, which ensures consistency with other self-contained systems, an extensive set of foundational libraries provided by the PlatformPlatform. The Shared Kernel and Account Management are connected through source code references, removing the need to manage NuGet packages for this purpose.

## Architecture and Design
Following the architecture and design principles of Domain-Driven Design (DDD), Command Query Responsibility Segregation (CQRS), Clean Code, and Clean Architecture, the Account Management system ensures a robust and maintainable codebase.

## Testing
Tests for the Account Management system are conducted using XUnit and SQLite for in-memory database testing. The tests can be run directly in JetBrains Rider, Visual Studio, or with the dotnet test command.

## Development Workflow
Developers can work on the Account Management system by opening the AccountManagement.sln solution file. As this is part of a mono repository, updates across Shared Kernel and Account Management are synchronized, ensuring consistency and reducing maintenance tasks.

## Future Scope
Future development plans include adding an App project, serving as a raw web-client, to the solution. This client will not have any .NET dependencies, offering more flexibility in terms of technology choices on the client side.