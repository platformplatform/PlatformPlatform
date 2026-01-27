# Application code

The application code for PlatformPlatform is divided into different self-contained systems. A self-contained system is a large microservice (or a small monolith) that contains a full vertical slice of the system, including a React frontend plus a .NET API and .NET Workers that share the same core. A self-contained system can be developed and tested in isolation from other self-contained systems, and all parts of a self-contained system are deployed together. This makes self-contained systems a better option than having many small microservice APIs that share one big frontend.

These are the self-contained systems:
- `account-management` - a self-contained system used for managing signups, tenants, users, logins, etc. While still work in progress the plan is that you will be able to use this as is, and focus on building your own `[your-saas-product]`.
- `back-office` - a self-contained system for operations and support. Currently empty, but showcases how to add new self-contained systems. The back-office will only be hosted in one cluster (e.g. West Europe), while other self-contained systems are hosted in all clusters.
- `[your-saas-product]` - create your own self-contained system for your SaaS product.

While self-contained systems are somewhat similar to a microservice architecture, the point of PlatformPlatform is not to create a distributed system. Since PlatformPlatform has all the core functionalities of a SaaS solution, the idea is that you as a consumer will create your own self-contained systems and only make minimal changes to existing ones like Account Management and Back Office. Ideally, you should have only one self-contained system unless you have a strong reason to create multiple. Remember, microservices are a solution to scale teams, not systems.

There are also some shared projects:
- `SharedKernel` - a foundation with generic functionalities and boilerplate code that are shared between self-contained systems. This ensures a secure and maintainable codebase. This not only guarantees a consistent architecture but also ensures that all self-contained systems are developed in a uniform manner, making it easy for developers to move between systems and focus on the business logic, rather than the infrastructure. In theory the shared kernel is maintained by the PlatformPlatform team, and there should be no reason for you to make changes to this project.
- `AppGateway` - the single entry point for all self-contained systems, responsible for routing requests to the correct system using YARP reverse proxy as BFF (Backend for Frontend). It contains logic for refreshing access tokens, and it will eventually also handle tasks like, rate limiting, caching, etc.
- `AppHost` - only used for development, this is a Aspire App Host that orchestrates starting all dependencies like SQL Server, Blob Storage, and Mail Server, and then starts all self-contained systems in a single operation. It’s a .NET alternative to Docker Compose. While Aspire can also be used for the deployment of infrastructure, this is not used in PlatformPlatform, as it’s not mature for enterprise-grade systems. If your self-contained system needs access to a different service, you can add it to the `AppHost` project.

## Account Management

Account Management currently offers a skeleton of the essential parts of any multi-tenant SaaS solution, like allowing a business to sign up for new tenants, invite users, let users log in, etc. Eventually, it will contain features to showcase single sign-on (SSO), subscription management, etc. that are common to all SaaS solutions. As of now, it just showcases how to build a system using Vertical Slice Architecture with CQRS, DDD, ASP.NET Minimal API, SPA frontend, orchestrated with Aspire.

The [AccountManagement.slnf](/application/AccountManagement.slnf) solution file contains the Account Management system, which can be run and developed in isolation. This shows how simple it is to develop new features without all the boilerplate you often see in other projects.

Self-contained systems in PlatformPlatform are divided into the following core projects following the design principles of Vertical Slice Architecture, Domain-Driven Design (DDD), and Command Query Responsibility Segregation (CQRS):

1. `WebApp`: The WebApp is built with React, ShadCN 2.0 with Base UI, Rsbuild, Turborepo, and more. It’s completely separated from the Backend, ensuring that it can be developed in isolation using e.g. Visual Studio Code (or Rider or Visual Studio).

2. `Api`: Built with ASP.NET Minimal API, this project implements the REST API. The API also serves the `index.html` from the SPA as a fallback (if no server endpoint was matched). This eliminates the need for separate web server to serve the static frontend. This, in turn, also means the frontend and API are guaranteed to be in sync. When the SPA `index.html` is served it injects environment configurations (app version, CDN URLs) and user info to avoid an extra API call.

    All API endpoints are extremely thin, with only one line of code in each endpoint, delegating the work to the Core layer:

    ```csharp
    group.MapPost("/", async Task<ApiResult> (CreateUserCommand command, ISender mediator)
        => await mediator.Send(command)
    );
    ```

3. `Core`: The Core layer consists of the use cases of the system implemented as MediatR commands and queries. Each command and query is a vertical slice of the system, containing all the logic needed to complete a task. This layer is also responsible for validation using FluentValidation. Here’s an example showing the CreateUser command, its handler, and its validator. Note that the command, handler, and validator are all in the same file. This aligns with the Single Responsibility Principle (SRP), making the code easy to understand and more maintainable.


   `/Core/Users/CreateUser.cs`

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
                .WithMessage(x => $"The user with '{x.Email}' already exists.")
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

    `/Core/Users/Domains/User.cs`

    ```csharp
    [IdPrefix("usr")]
    public sealed record UserId(string Value) : StronglyTypedUlid<UserId>(Value);

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

    `/Core/Users/Domain/UserRepository.cs`

    ```csharp
    public interface IUserRepository : ICrudRepository<User, UserId>
    {
        Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken);
    }

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

The architecture is designed according to [screaming architecture](https://blog.cleancoder.com/uncle-bob/2011/09/30/Screaming-Architecture.html). This means that there is one namespace (folder) per feature, using business-related names like `Tenants` and `Users`, making the concepts easily visible and expressive, rather than organizing the code by types like `models`, `services`, `repositories`, etc. Not only does this make the code easier to understand, but it also makes it easier to split the system into self-contained systems in the future.

Tests for the Account Management system are conducted using xUnit, with SQLite for in-memory database testing. The tests can be run directly in JetBrains Rider, Visual Studio, or with the `pp test` command. The tests focus on the behavior of the system, not the implementation details. This is done by focusing on testing the API instead of Application and Domain classes when possible.

