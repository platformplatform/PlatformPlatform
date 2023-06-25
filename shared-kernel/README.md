# Shared Kernel

## Overview

The Shared Kernel is a set of foundational libraries that serve as a common base for the various self-contained systems within the PlatformPlatform. It provides a consistent set of functionalities and maintains a uniform architecture across different self-contained systems, enabling seamless integration and ensuring maintainability of the codebase. 

## Structure

The Shared Kernel is divided into four core projects, each serving a distinct purpose:

1. `ApiCore`: Provides a set of common functionalities for the API layer.
2. `ApplicationCore`: Houses common functionalities for the Application layer, like MediatR behaviour pipelines for UnitOfWork, Validation, and publishing domain events. 
3. `DomainCore`: Contains bases classes for DDD like AggregateRoot, Audible entty, stongly typed ID base classe, and ID Generation unike cronological IDS (Ulid and longs).
4. `InfrastructureCore`: Holds shared functionality for the Infrastructure layer like Entity framework core helpers like RepositoryBas, logic to ensre entites timestamp are updated, that enums are saved as strings, configuration helpers, and more.

There's also a `Tests` project which includes unit and integration tests for each of the above projects, ensuring the reliability of the Shared Kernel.

## Architecture and Design

Shared Kernel follows principles from Domain-Driven Design (DDD), Command Query Responsibility Segregation (CQRS), and Clean Architecture. This enables each self-contained system to leverage a uniform structure and base functionalities, reducing overhead and facilitating maintainability.

## Development Workflow

Developers can work on the Shared Kernel by opening the `SharedKernel.sln` solution file. Being part of a mono repository, the Shared Kernel synchronizes updates across all the self-contained systems, ensuring consistency, reducing overhead and maintenance tasks.

## Testing

Shared Kernel's tests are conducted using XUnit, with separate test cases for each project in the Shared Kernel. These tests ensure the robustness and reliability of the foundational libraries provided by the Shared Kernel.

## Future Scope

The Shared Kernel serves as a living architecture base, growing, and adapting to the needs of the self-contained systems. As such, it will continuously evolve with the rest of the platform.