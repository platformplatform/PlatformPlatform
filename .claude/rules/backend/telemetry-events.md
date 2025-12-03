---
trigger: glob
globs: **/Commands/*.cs,TelemetryEvents.cs
description: Rules for telemetry events including important rules of where to create events, naming, and what properties to track
---

# Telemetry Events

Guidelines for implementing telemetry events in the backend, including event structure, naming, and publishing practices.

## Implementation

1. Create telemetry events to collect information about application behavior and usage, helping stakeholders understand how the application is used
2. Always create telemetry events in `/[scs-name]/Core/TelemetryEvents.cs` to maintain consistent naming
3. Name telemetry events in past tense (e.g., `LoginCompleted`, `UserCreated`) and inherit from `TelemetryEvent`
4. Ensure events are sorted alphabetically in the `TelemetryEvents.cs` file
5. Use primary constructors to define event properties
6. Include relevant contextual information for better insights:
   - Include the ID of aggregates and entities when performing mutations (UserId and TenantId are tracked from execution context)
   - Example: in `LoginCompleted`, include `UserId` and `LoginTimeInSeconds` to measure login duration
7. Use snake_case for property names in event data to align with OpenTelemetry conventions
8. Collect events using `events.CollectEvent()` in command handlers just before returning
9. Events are only collected for successful commands by default—set `commitChanges: true` for failed commands
10. Don't track IDs of many-to-many aggregates—track the IDs of the two main aggregates instead
11. Don't track IDs of single-use operations that are merely unique identifiers, as they provide no analytical value and may contain PII

Note: Telemetry events are automatically annotated with extra data from the request, including current tenant ID, authenticated user ID, user role, application version, user's location, device type, browser, etc.

## Examples

### Example 1 - Telemetry Event Definitions

```csharp
// ✅ DO: Use past tense naming and snake_case for property names
public sealed class EmailConfirmationFailed(EmailConfirmationId emailConfirmationId, EmailConfirmationType emailConfirmationType, int retryCount)
    : TelemetryEvent(("email_confirmation_id", emailConfirmationId), ("email_confirmation_type", emailConfirmationType), ("retry_count", retryCount));

public sealed class LoginCompleted(UserId userId, int loginTimeInSeconds)
    : TelemetryEvent(("user_id", userId), ("login_time_in_seconds", loginTimeInSeconds));

public sealed class UserRoleChanged(UserId userId, UserRole fromRole, UserRole toRole)
    : TelemetryEvent(("user_id", userId), ("from_role", fromRole), ("to_role", toRole));

// ❌ DON'T: Use present tense or collect personal information
public sealed class CompleteLogin(LoginID loginId, UserId userId, string email, string ipAddress) // ❌ LoginId is not meaningful in events, Email and IP are PII data
    : TelemetryEvent(("user_id", userId), ("email", email), ("ip_address", ipAddress));
```

### Example 2 - Using Telemetry Events in Command Handlers

```csharp
// ✅ DO: Collect events just before returning and use commitChanges for failed commands
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

// ❌ DON'T: Collect events throughout the method or forget to use commitChanges for failed commands
public async Task<Result> Handle(BadCompleteLoginCommand command, CancellationToken cancellationToken)
{
    // Wrong: collecting events too early
    events.CollectEvent(new LoginStarted(command.Id));
    
    // Business logic...
    
    if (login.HasExpired())
    {
        // Wrong: missing commitChanges: true, so event won't be published
        return Result.BadRequest("The code is no longer valid.");
    }

    return Result.Success();
}
```
