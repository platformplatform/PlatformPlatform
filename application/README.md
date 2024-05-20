# Application code

The application code for PlatformPlatform is divided into different self-contained systems. A self-contained system is a large microservice (or a small monolith) that contains a full vertical slice of the system, including a React frontend plus a .NET API and .NET Workers that share the same application, domain and infrastructure. A self-contained system can be developed and tested, but all parts of a self-contained system will are deployed together. This makes self-contained systems a better option than having many small microservices APIs, that has one big shared frontend.

These are the self-contained systems:
- `account-management` - a self-contained system used for managing tenants, users, etc. While still work in progress the plan is that you will be able to use this as is, and focus on building your own `[your-saas-product]`.
- `back-office` - a self-contained system for operations and support. Currently empty, but showcases how to add new self-contained systems. The back-office will only be hosted in one cluster (e.g. West Europe), while other self-contained systems is hosted in all clusters.
- `[your-saas-product]` - create your own self-contained system for your SaaS product.

While self-contained systems are a bit like a microservice architecture, the point of PlatformPlatform is not to create a distributed system. But since PlatformPlatform has all the core functionalities of a SaaS solution, the idea is that consumers will create their own self-contained systems and only do minimal changes to e.g. Account Management and Back Office., and ideally only one, so don't create multiple self-contained systems unless you have a good reason to do so. Remember mircoservices are a solution to scale teams, not systems.

There are also some shared projects:
- `shared-kernel` - a foundation with generic functionalities and boilerplate code that are shared between self-contained systems
- `AppGateway` - the AppGateway is the single entry point for all self-contained systems, and is responsible for routing requests to the correct self-contained system using YARP reverse proxy as BFF (Backend for Frontend). It will eventually also be used for e.g. refreshing access tokens, rate limiting, caching, etc.
- `AppHost` - only used for development, this is a .NET Aspire App Host that orchestrates starting all dependencies like SQL Server, Blob Storage, and Mail Server, and then starting all self-contained systems in a single operation. It's a .NET alternative to Docker Compose. While Aspire can also be used for deployment of infrastructure, this is not used in PlatformPlatform, as it's not mature for enterprise-grade systems.

## Account Management

Account Management currently offers a skeleton of the essential parts of any multi-tenant SaaS solution, like creating tenants, users, etc. Eventually, it will contain features to showcase single sign-on (SSO), subscription management, etc. that are common to all SaaS solutions. As of now, it just showcases how to build a clean architecture system with CQRS, DDD, ASP.NET Minimal API, SPA frontend, orchestrated with .NET Aspire.

The [AccountManagement.slnf](/application/AccountManagement.slnf) solution file contains the Account Management system, which can be run and developed in isolation. This shows how simple it is to develop new features without all the boilerplate you often see in other projects.

Self-contained systems in PlatformPlatform are divided into four core projects following the design principles of Clean Architecture, Domain-Driven Design (DDD), and Command Query Responsibility Segregation (CQRS):

1. `WebApp`: The WebApp is built with React, React Area Components, Rspack, Yarn, and more. It's completely separated from the Backend, ensuring that it can be developed in isolation using e.g. Visual Studio Code (or Rider or Visual Studio).

2. `Api`: Built with ASP.NET Minimal API, this project implements the REST API. The API also serves the `index.html` from the SPA using a middleware that injects environment configurations (app version, CDN URLs) and user info to avoid an extra API call. This, in turn, also means the frontend and API are guaranteed to be in sync. All API endpoints are extremely thin, with only one line of code in each endpoint, delegating the work to the Application layer. Here's an example of an API endpoint that creates a new user:

    ```csharp
    group.MapPost("/", async Task<ApiResult> (CreateUserCommand command, ISender mediator)
        => await mediator.Send(command)
    );
    ```

3. `Application`: The Application layer consists of the use cases of the system implemented as MediatR commands and queries. Each command and query is a vertical slice of the system, containing all the logic needed to complete a task. This layer is also responsible for validation using FluentValidation. Here's an example showing the CreateUser command, its handler, and its validator. Note that the command, handler, and validator are all in the same file. This aligns with the Single Responsibility Principle (SRP), making the code easy to understand and more maintainable.

    ```csharp
    public sealed record CreateUserCommand(TenantId TenantId, string Email, UserRole UserRole)
        : ICommand, IUserValidation, IRequest<Result<UserId>>;

    public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserValidator(IUserRepository userRepository, ITenantRepository tenantRepository)
        {
            RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
            RuleFor(x => x)
                .MustAsync((x, cancellationToken)=> userRepository.IsEmailFreeAsync(x.TenantId, x.Email, cancellationToken))
                .WithName("Email")
                .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }

    public sealed class CreateUserHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
        : IRequestHandler<CreateUserCommand, Result<UserId>>
    {
        public async Task<Result> Handle(CreateUserCommand command, CancellationToken cancellationToken)
        {
            var user = User.Create(command.TenantId, command.Email, command.UserRole);
            await userRepository.AddAsync(user, cancellationToken);

            events.CollectEvent(new UserCreated(command.TenantId));

            return Result.Success();
        }
    }
    ```

4. `Domain`: The Domain layer contains the domain model, which is the heart of the system. It contains the aggregates, entities, value objects, repository interfaces, and domain events. Here's an example of the User aggregate:

    ```csharp
    public sealed class User : AggregateRoot<UserId>
    {
        private User(TenantId tenantId, Email email, UserRole userRole) : base(UserId.NewId())
        {
            TenantId = tenantId;
            Email = email;
            UserRole = userRole;
        }

        public TenantId TenantId { get; }

        public Email Email { get; private set; }

        public UserRole UserRole { get; private set; }

        public static User Create(TenantId tenantId, Email email, UserRole userRole)
        {
            var user = new User(tenantId, email, userRole);
            user.AddDomainEvent(new UserCreatedEvent(user.Id));
            return user;
        }
    }
    ```

5. `Infrastructure`: This layer contains the implementation of the interfaces defined in the Domain and Application layer for accessing external resources. Specifically, it contains the implementation of the repositories defined in the Domain layer, as well as the Entity Framework (EF) database context, migrations, and the like. Here's an example of the UserRepository:

    ```csharp
    internal sealed class UserRepository(AccountManagementDbContext accountManagementDbContext)
        : RepositoryBase<User, UserId>(accountManagementDbContext), IUserRepository
    {
        public async Task<bool> IsEmailFreeAsync(TenantId tenantId, Email email, CancellationToken cancellationToken)
        {
            return !await DbSet.AnyAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
        }
    }
    ```

Please note that all IDs are strongly typed (like `TenantId` and `UserId`), even at the API endpoints. This ensures a more expressive codebase.

The architecture is designed according to [screaming architecture](https://blog.cleancoder.com/uncle-bob/2011/09/30/Screaming-Architecture.html). This means that there is one namespace (folder) per feature, using business-related names like `Tenants` and `Users`, making the concepts easily visible and expressive, rather than organizing the code by types like `models`, `services`, `repositories`, etc. Not only does this make the code easier to understand, it also makes it easier to split the system into self-contained systems in the future.

Below is a visual representation of the layers. An important constraint is that inner layers cannot reference outer layers. This means that the Domain layer has no knowledge of other layers, and the Application layer only knows about the Domain layer. Also, the API and Infrastructure layer cannot reference each other. However, to allow dependency injection registration, the startup class [Program.cs](/application/account-management/Api/Program.cs) in API calls the Infrastructure layer.

<p align="center">
  <img src="https://platformplatformgithub.blob.core.windows.net/CleanArchitecture.png" alt="Clean Architecture">
</p>

Tests for the Account Management system are conducted using xUnit, with SQLite for in-memory database testing. The tests can be run directly in JetBrains Rider, Visual Studio, or with the `pp test` command. The tests focus on the behavior of the system, not the implementation details. This is done by focusing on testing the API instead of Application and Domain classes when possible.

## Shared Kernel

The Shared Kernel provides generic functionality across all self-contained systems, ensuring a secure and maintainable codebase. This not only guarantees a consistent architecture but also ensures that all self-contained systems are developed in a uniform manner, making it easy for developers to move between systems and focus on the business logic, rather than the infrastructure.

The Shared Kernel is divided into four core projects that align with the DDD and clean architecture layers:

1. `ApiCore`: Provides a set of common functionalities for the API layer. It's an `IsAspireSharedProject` project that sets up service defaults for resilience, health checks, service discovery. It enforces HTTPS, global exception handling, API result types, conversion of Strongly Typed IDs & Enums to and from strings, and ensures all failed API requests are wrapped in a `ProblemDetails` object.

2. `ApplicationCore`: Houses common functionalities for the Application layer, like MediatR behaviour pipelines for UnitOfWork, Validation, publishing domain events, and a base class for MediatR commands and queries.

3. `DomainCore`: Contains base classes for tactically implementing DDD patterns like AggregateRoot, Entity, Repository, DomainEvent, etc. It also contains a base class for strongly typed ID, and unique chronological ID generation.

4. `InfrastructureCore`: Holds shared functionality for the Infrastructure layer like Entity framework core helpers like RepositoryBase, logic to ensure entities timestamp are updated, that enums are saved as strings, configuration helpers, and more.

The Shared Kernel serves as a living architecture base, growing and adapting to the needs of self-contained systems. As such, it will continuously evolve with the rest of the platform.
