# Telemetry Events

Telemetry events collect information about the application's behavior and usage. They help business stakeholders understand how the application is being used and gather valuable insights. In production, Azure Application Insights collects these events along with other metrics and logs.

## Structure

Telemetry events should always be created in the existing `/[scs-name]/Core/TelemetryEvents.cs` file. Keeping all telemetry events in one place makes it easier to maintain high quality and consistent naming. This is especially important because events cannot be changed once they have been collected.

## Implementation

1. Telemetry events should be named in past tense and inherit from `TelemetryEvent`:
   - Ensure events are sorted alphabetically in `TelemetryEvents.cs` file.
2. Use primary constructors to define the event properties:
   - IMPORTANT: Include relevant contextual information in the event that allows for better insights. E.g., for `LoginCompleted` include `UserId` and `LoginTimeInSeconds` to measure how long it takes users to complete login.
3. Use snake_case for property names in event data to align with OpenTelemetry conventions.
4. Collect events using the `events.CollectEvent()` method in command handlers just before returning. The PublishTelemetryEventsPipelineBehavior MediatR pipeline behavior will automatically publish these events after the UnitOfWork is completed.
5. By default, events are only collected for successful commands. To collect events for failed commands, set `commitChanges: true` in the Result, e.g. `return Result.BadRequest("The code is no longer valid.", commitChanges: true);`.
6. Never collect any personal information like email, phone number, title in events, as these can never be removed once collected.

Note: Telemetry events are automatically annotated with extra data from the request, including current tenant ID, authenticated user ID, user role, application version, user's location, device type, browser, etc.

## Example

```csharp
public sealed class EmailConfirmationFailed(EmailConfirmationId emailConfirmationId, EmailConfirmationType emailConfirmationType, int retryCount)
    : TelemetryEvent(("email_confirmation_id", emailConfirmationId), ("email_confirmation_type", emailConfirmationType), ("retry_count", retryCount));

public sealed class LoginCompleted(UserId userId, int loginTimeInSeconds)
    : TelemetryEvent(("user_id", userId), ("login_time_in_seconds", loginTimeInSeconds));

public sealed class UserRoleChanged(UserId userId, UserRole fromRole, UserRole toRole)
    : TelemetryEvent(("user_id", userId), ("from_role", fromRole), ("to_role", toRole));
```

## Usage in Command/Query Handlers

```csharp
public async Task<Result> Handle(CompleteLoginCommand command, CancellationToken cancellationToken)
{
    // Business logic...
    
    if (login.HasExpired())
    {
        events.CollectEvent(new LoginExpired(login.UserId, login.SecondsSinceStarted));
        return Result.BadRequest("The code is no longer valid.", commitChanges: true);
    }

    // More business logic...

    events.CollectEvent(new LoginCompleted(user.Id, login.SecondsSinceStarted));

    return Result.Success();
}
```
