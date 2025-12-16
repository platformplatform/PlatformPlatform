---
paths: **/Commands/*.cs
description: Rules for implementing CQRS commands, validation, handlers, and structure
---

# CQRS Commands

Guidelines for implementing CQRS commands, including structure, validation, handlers, and MediatR pipeline behaviors.

## Structure

Commands should be created in the `/[scs-name]/Core/Features/[Feature]/Commands` directory.

## Implementation

1. Create one file per command containing `Command`, `Validator`, and `Handler`:
   - Name the file after the command without suffix
2. Command Record:
   - Create a public sealed record marked with `[PublicAPI]` that implements `ICommand` and `IRequest<Result>` or `IRequest<Result<T>>`
   - Name with `Command` suffix
   - Define properties in the primary constructor
   - Use property initializers for simple input sanitization (trimming, casing)
   - For route parameters, use `[JsonIgnore] // Removes from API contract` on real properties, not primary constructor parameters
3. Command validator:
   - Only validate if the command has user input
   - Each property should have one shared validation message for all cases (required, max length, etc.)
   - Don't inject dependencies like repositories—use guards in the handler instead
   - Only validate user input, not route parameters, enum values, or strongly typed IDs validated by the model binder
4. Handler:
   - Create a public sealed class with `Handler` suffix
   - Implement `IRequestHandler<CommandType, Result>` or `IRequestHandler<CommandType, Result<T>>`
   - Commands can optionally return a newly created ID, but only if truly needed
   - Use guard statements with early returns like `Result.BadRequest()`, `Result.NotFound()`
     - Enclose dynamic values in single quotes and end messages with a period
   - Never throw exceptions—always return `Result.Xxx()`
   - Always create [Telemetry Events](/.claude/rules/backend/telemetry-events.md) for successful command results
     - Optionally log telemetry for failed commands when it adds business value
     - Prefer one event per command; for bulk operations, track individual events if single operation equivalents exist
   - Save changes:
     - Call `AddAsync()`, `Remove()`, `Update()` to persist changes
     - Never call `SaveChangesAsync()` directly
   - Never do N+1 operations—load all entities and process them in memory
5. Command Composition:
   - Inject MediatR to chain commands: `await mediator.Send(new CreateUserCommand(...))`
   - Extract shared logic to `/[scs-name]/Core/Features/[Feature]/Shared`

Note: Commands run through MediatR pipeline behaviors in this order: Validation → Command → PublishDomainEvents → UnitOfWork → PublishTelemetryEvents. Nested commands and domain events are handled within the UnitOfWork transaction. Also, note that Entity Framework change tracking is disabled.

## Example

```csharp
// CreateUser.cs
public sealed record CreateUserCommand(string Email, string Name)
    : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract // ✅ DO: Add JsonIgnore for route parameters 
    public TenantId TenantId { get; init; } = null!;

    // ✅ DO: Normalize input in property initializers
    public string Email { get; } = Email.Trim().ToLower();
}

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        // ✅ DO: Use the same message for better user experience and easier localization
        RuleFor(x => x.Name).Length(1, 50).WithMessage("Name must be between 1 and 50 characters.");
    }
}

public sealed class CreateUserHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<CreateUserCommand, Result>
{
    public async Task<Result> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // ✅ DO: Use guard statements with early returns
        if (await userRepository.IsEmailFreeAsync(command.Email, cancellationToken) == false)
        {
            return Result.BadRequest($"User with email '{command.Email}' already exists.");
        }

        var user = User.Create(command.Email, command.Name);
        await userRepository.AddAsync(user, cancellationToken);

        // ✅ DO: Always collect telemetry events
        events.CollectEvent(new UserCreated(user.Id, user.Avatar.IsGravatar));

        return Result.Success();
    }
}
```

```csharp
public sealed record CreateUserCommand([JsonIgnore] TenantId TenantId; string Email) // ❌ DON'T: Add attributes on positional parameters (“primary constructor parameters”)
    : ICommand, IRequest<Result>;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        // ❌ DON'T: Use different validation messages for the same property and redundant validation rules
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name must not be empty.")
            .MaximumLength(50).WithMessage("Name must not be more than 50 characters.");
    }
}

public sealed class CreateUserHandler(
    ITelemetryEventsCollector events, // ❌ DON'T: Place generic dependencies before specific ones
    IUserRepository userRepository,
    SendEmailHandler sendEmailHandler // ❌ DON'T: Inject handlers directly
): IRequestHandler<CreateUserCommand, Result>
{
    public async Task<Result> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // ❌ DON'T: Perform validation in the handler that should be in the validator
        if (!command.Email.Contains('@'))
        {
            // ❌ Forgetting to enclose values in single quotes and trailing period
            throw new ArgumentException($"Email {command.Email} must be valid"); // ❌ DON'T: Throw exceptions
        }

        if (someCondition)
        {
            return Result.BadRequest( // ❌ DON'T: Split Result returns across multiple lines if it fits on one line
                $"User with email {command.Email} already exists" // ❌ Missing single quotes around dynamic value and trailing period
            );
        }

        // ❌ DON'T: Call handlers directly instead of using MediatR or raise domain events
        await sendEmailHandler.Handle(new SendEmailCommand(command.Email, "Welcome!"), cancellationToken);

        // ❌ DON'T: Forget to track telemetry events

        return Result.Success();
    }
}
```
