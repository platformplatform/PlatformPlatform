# Application code

The application code for PlatformPlatform is divided into two parts:

-   `account-management` - a self-contained system used for managing tenants, users, etc.
-   `shared-kernel` - a foundation with generic functionalities and boilerplate code that are shared between self-contained systems

A self-contained system is a large microservice (or a small monolith) that contains the full stack, including frontend, API, background jobs, domain logic, etc. These can be developed, tested, deployed, and scaled in isolation, making it a good compromise between a large monolith and many small microservices. Unlike the popular backend-for-frontend (BFF) style with one shared frontend, this allows teams to work fully independently.

While PlatformPlatform is currently a monolith, as it only has one self-contained system, it is prepared for a future where it can be easily split into more self-contained systems.

## Account Management

Account Management currently offers a skeleton of the essential parts of any multi-tenant SaaS solution, like creating tenants, users, etc. Eventually, it will contain a frontend, with single sign-on (SSO), subscription management, etc. that are common to all SaaS solutions. As of now, it just showcases how to build a clean architecture system with CQRS, DDD, and ASP.NET Minimal API.

The [AccountManagement.sln](/application/account-management/AccountManagement.sln) solution file contains the Account Management system, which can be run and developed in isolation. This shows how simple it is to develop new features without all the boilerplate you often see in other projects.

Self-contained systems in PlatformPlatform are divided into four core projects following the design principles of Clean Architecture, Domain-Driven Design (DDD), and Command Query Responsibility Segregation (CQRS):

1. `Api`: Built with ASP.NET Minimal API, this project implements the REST API. All API endpoints are extremely thin, with only one line of code in each endpoint, delegating the work to the Application layer. Here's an example of an API endpoint that creates a new user:

    ```csharp
     group.MapPost("/api/users", async Task<ApiResult> (CreateUser.Command command, ISender mediator)
         => await mediator.Send(command);
    ```

2. `Application`: The Application layer consists of the use cases of the system implemented as MediatR commands and queries. Each command and query is a vertical slice of the system, containing all the logic needed to complete a task. This layer is also responsible for validation using FluentValidation. Here's an example showing the CreateUser command, its handler, and its validator. Note that the command, handler, and validator are all in the same file, using a static class. This aligns with the Single Responsibility Principle (SRP), making the code easy to understand and more maintainable.

    ```csharp
    public static class CreateUser
    {
        public sealed record Command(TenantId TenantId, string Email, UserRole UserRole)
            : ICommand, IUserValidation, IRequest<Result<UserId>>;

        public sealed class Handler : IRequestHandler<Command, Result<UserId>>
        {
            private readonly IUserRepository _userRepository;

            public Handler(IUserRepository userRepository)
            {
                _userRepository = userRepository;
            }

            public async Task<Result<UserId>> Handle(Command command, CancellationToken cancellationToken)
            {
                var user = User.Create(command.TenantId, command.Email, command.UserRole);
                await _userRepository.AddAsync(user, cancellationToken);
                return user.Id;
            }
        }

        public sealed class Validator : AbstractValidator<Command>
        {
            public Validator(IUserRepository userRepository, ITenantRepository tenantRepository)
            {
                RuleFor(x => x.TenantId)
                    .MustAsync(async (tenantId, cancellationToken) =>
                        await tenantRepository.ExistsAsync(tenantId, cancellationToken))
                    .WithMessage(x => $"The tenant '{x.TenantId}' does not exist.");

                RuleFor(x => x.Email)
                    .NotEmpty()
                    .EmailAddress()
                    .MaximumLength(100)
                    .WithMessage("Email must be in a valid format and no longer than 100 characters.");

                RuleFor(x => x)
                    .MustAsync(async (x, cancellationToken)
                        => await userRepository.IsEmailFreeAsync(x.TenantId, x.Email, cancellationToken))
                    .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.");
            }
        }
    }
    ```

3. `Domain`: The Domain layer contains the domain model, which is the heart of the system. It contains the aggregates, entities, value objects, repository interfaces, and domain events. Here's an example of the User aggregate:

    ```csharp
    public sealed class User : AggregateRoot<UserId>
    {
        private User(TenantId tenantId, string email, UserRole userRole) : base(UserId.NewId())
        {
            TenantId = tenantId;
            Email = email;
            UserRole = userRole;
        }

        public TenantId TenantId { get; }

        public string Email { get; private set; }

        public UserRole UserRole { get; private set; }

        public static User Create(TenantId tenantId, string email, UserRole userRole)
        {
            var user = new User(tenantId, email, userRole);
            user.AddDomainEvent(new UserCreatedEvent(user.Id));
            return user;
        }
    }
    ```

4. `Infrastructure`: This layer contains the implementation of the interfaces defined in the Domain and Application layer for accessing external resources. Specifically, it contains the implementation of the repositories defined in the Domain layer, as well as the Entity Framework database context, migrations, and the like. Here's an example of the UserRepository:

    ```csharp
    internal sealed class UserRepository : RepositoryBase<User, UserId>, IUserRepository
    {
        public UserRepository(AccountManagementDbContext accountManagementDbContext) : base(accountManagementDbContext)
        {
        }

        public async Task<bool> IsEmailFreeAsync(TenantId tenantId, string email, CancellationToken cancellationToken)
        {
            return !await DbSet.AnyAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
        }
    }
    ```

Please note that all IDs are strongly typed (like `TenantId` and `UserId`), even at the API endpoints. This ensures a more expressive codebase.

The architecture is designed following [screaming architecture](https://blog.cleancoder.com/uncle-bob/2011/09/30/Screaming-Architecture.html). This means that there is one namespace (folder) per feature, using names from the business like `Tenants` and `Users`, making the concepts easily visible and expressive, rather than organizing the code by types like `models`, `services`, `repositories`, etc. Not only does this make the code easier to understand, it also makes it easier to split the system into self-contained systems in the future.

Below is a visual representation of the layers. An important constraint is that inner layers cannot reference outer layers. This means that the Domain layer has no knowledge of other layers, and the Application layer only knows about the Domain layer. Also, the API and Infrastructure layer cannot reference each other. However, to allow dependency injection registration, the startup class [Program.cs](/application/account-management/Api/Program.cs) in API calls the Infrastructure layer.

<p align="center">
  <img src="https://camo.githubusercontent.com/a1eb8809505b6ff0f3c76861c75df8e48faa1805b5fb7a13aa25cefc9654dce2/68747470733a2f2f6d656e74756d74656d702e626c6f622e636f72652e77696e646f77732e6e65742f7075626c69632f436c65616e4172636869746563747572652e706e67" alt="Clean Architecture">
</p>

Tests for the Account Management system are conducted using xUnit, with SQLite for in-memory database testing. The tests can be run directly in JetBrains Rider, Visual Studio, or with the `dotnet test` command. The tests focus on the behavior of the system, not the implementation details. In fact, over 90% code coverage is achieved by testing only the API.

## Shared Kernel

The Shared Kernel provides generic functionality across all self-contained systems, ensuring a secure and maintainable codebase. This not only guarantees a consistent architecture, but also ensures that all self-contained systems are developed in a uniform manner, making it easy for developers to move between systems and focus on the business logic, rather than the infrastructure.

The Shared Kernel is divided into four core projects that align with the DDD and clean architecture layers:

1. `ApiCore`: Provides a set of common functionalities for the API layer, like enforcing HTTPS, global exception handling, API result types, conversion of Strongly Typed IDs & Enums to and from strings, and ensuring all failed API requests are wrapped in a `ProblemDetails` object.
2. `ApplicationCore`: Houses common functionalities for the Application layer, like MediatR behaviour pipelines for UnitOfWork, Validation, publishing domain events, and a base class for MediatR commands and queries,
3. `DomainCore`: Contains base classes for tactically implementing DDD patterns like AggregateRoot, Entity, ValueObject, DomainEvent, etc. It also contains a base class for strongly typed ID, and unique chronological ID generation.
4. `InfrastructureCore`: Holds shared functionality for the Infrastructure layer like Entity framework core helpers like RepositoryBase, logic to ensure entities timestamp are updated, that enums are saved as strings, configuration helpers, and more.

The Shared Kernel serves as a living architecture base, growing and adapting to the needs of self-contained systems. As such, it will continuously evolve with the rest of the platform.
