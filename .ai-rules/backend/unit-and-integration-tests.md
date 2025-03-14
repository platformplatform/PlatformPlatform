# Writing Unit and Integration Tests

When writing tests for backend code, follow these rules very carefully.

## Naming Convention

1. Test files should be named `[Feature]/[Command|Query]Tests.cs` (e.g., `Users/GetUsersTests.cs`).
2. Test classes should be named `[Command|Query]Tests` and be `sealed`.
3. Test methods should follow this pattern: `[Method]_[Condition]_[ExpectedResult]`.

For example:
- `CompleteLogin_WhenInvalidOneTimePassword_ShouldReturnBadRequest`
- `GetUsers_WhenSearchingBasedOnUserEmail_ShouldReturnUser`

## Test Organization

1. Organize tests by feature area in directories matching the feature structure.
2. Create architecture tests for enforcing code style and structure patterns.
3. For endpoint tests, inherit from `EndpointBaseTest<TContext>` for access to HTTP clients and test infrastructure.

## Implementation

1. Prefer creating API Tests to test behavior over implementation:
   - Use `AuthenticatedOwnerHttpClient` or `AuthenticatedMemberHttpClient` for authenticated requests.
   - Use `AnonymousHttpClient` for anonymous requests.
2. Use xUnit with `[Fact]` attribute or `[Theory]` if multiple test cases are needed.
3. Use FluentAssertions for clear assertion syntax.
4. Use NSubstitute for mocking external dependencies but never mock repositories.
5. Follow the Arrange-Act-Assert pattern with clear comments for each section.
6. Test both happy path and error cases.
7. Verify side effects like database changes and telemetry events.

IMPORTANT: Pay special attention to ensure consistent ordering, naming, spacing, line breaks of methods, parameters, variables, etc. For example, when creating SQL dummy data, ensure columns are in the exact same order as in the database. Or if you make several tests make sure things that are similar is written in the same way.

## Example 1 - Command Test

```csharp
[Fact]
public async Task CompleteLogin_WhenValid_ShouldCompleteLoginAndCreateTokens()
{
    // Arrange
    var (loginId, _) = await StartLogin(DatabaseSeeder.User1.Email);
    var command = new CompleteLoginCommand(CorrectOneTimePassword);

    // Act
    var response = await AnonymousHttpClient
        .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

    // Assert
    await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
    var updatedLoginCount = Connection.ExecuteScalar(
        "SELECT COUNT(*) FROM Logins WHERE Id = @id AND Completed = 1", new { id = loginId.ToString() }
    );
    updatedLoginCount.Should().Be(1);

    TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
    TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
    TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("LoginCompleted");
    TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.user_id"].Should().Be(DatabaseSeeder.User1.Id);
    TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

    response.Headers.Count(h => h.Key == "x-refresh-token").Should().Be(1);
    response.Headers.Count(h => h.Key == "x-access-token").Should().Be(1);
}
```

## Example 2 - Query Test

```csharp
[Fact]
public async Task GetUsers_WhenSearchingBasedOnUserEmail_ShouldReturnUser()
{
    // Arrange
    const string searchString = "willgate";

    // Act
    var response = await AuthenticatedMemberHttpClient.GetAsync($"/api/account-management/users?search={searchString}");

    // Assert
    response.ShouldBeSuccessfulGetRequest();
    var userResponse = await response.DeserializeResponse<GetUsersResponse>();
    userResponse.Should().NotBeNull();
    userResponse.TotalCount.Should().Be(1);
    userResponse.Users.First().Email.Should().Be(Email);
}
```

## Error Case Test Example

```csharp
[Fact]
public async Task CompleteLogin_WhenInvalidOneTimePassword_ShouldReturnBadRequest()
{
    // Arrange
    var (loginId, emailConfirmationId) = await StartLogin(DatabaseSeeder.User1.Email);
    var command = new CompleteLoginCommand(WrongOneTimePassword);

    // Act
    var response = await AnonymousHttpClient
        .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

    // Assert
    await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is wrong or no longer valid.");

    // Verify retry count increment and event collection
    var loginCompleted = Connection.ExecuteScalar("SELECT Completed FROM Logins WHERE Id = @id", new { id = loginId.ToString() });
    loginCompleted.Should().Be(0);
    var updatedRetryCount = Connection.ExecuteScalar("SELECT RetryCount FROM EmailConfirmations WHERE Id = @id", new { id = emailConfirmationId.ToString() });
    updatedRetryCount.Should().Be(1);

    TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
    TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
    TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailConfirmationFailed");
    TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
}
```

## Example 3 - Multiple Error Case Test

```csharp
[Fact]
public async Task UpdateCurrentUser_WhenInvalid_ShouldReturnBadRequest()
{
    // Arrange
    var command = new UpdateCurrentUserCommand
    {
        FirstName = Faker.Random.String(31),
        LastName = Faker.Random.String(31),
        Title = Faker.Random.String(51)
    };

    // Act
    var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync("/api/account-management/users/me", command);

    // Assert
    var expectedErrors = new[]
    {
        new ErrorDetail("firstName", "First name must be no longer than 30 characters."),
        new ErrorDetail("lastName", "Last name must be no longer than 30 characters."),
        new ErrorDetail("title", "Title must be no longer than 50 characters.")
    };
    await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
}
```

## Example 4 - Architecture Test

```csharp
[Fact]
public void PublicClassesInCore_ShouldBeSealed()
{
    // Act
    var types = Types
        .InAssembly(Configuration.Assembly)
        .That().ArePublic()
        .And().AreNotAbstract()
        .And().DoNotHaveName(typeof(Result<>).Name);

    var result = types
        .Should().BeSealed()
        .GetResult();

    // Assert
    var nonSealedTypes = string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>());
    result.IsSuccessful.Should().BeTrue($"The following are not sealed: {nonSealedTypes}");
}
```

## Example 5 - Setting Up Test Data

1. Use `DatabaseSeeder` for common test data
2. For feature-specific test data, set up in the test class constructor
3. Use SQL queries or EF Core operations for database manipulation

```csharp
public GetUsersTests()
{
    Connection.Insert("Users", [
            ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
            ("Id", UserId.NewId().ToString()),
            ("CreatedAt", DateTime.UtcNow.AddMinutes(-10)),
            ("ModifiedAt", null),
            ("Email", Email),
            ("FirstName", FirstName),
            ("LastName", LastName),
            ("Title", "Philanthropist & Innovator"),
            ("Role", UserRole.ToString()),
            ("EmailConfirmed", true),
            ("Avatar", JsonSerializer.Serialize(new Avatar())),
            ("Locale", "en-US")
        ]
    );
}
```

## Base Test Class Structure

The `EndpointBaseTest<TContext>` class provides:

1. Authenticated and anonymous HTTP clients for endpoint testing.
2. In-memory SQLite database for test isolation.
3. Service mocking with NSubstitute.
4. Telemetry event collection for verifying events.
5. Proper test cleanup with the Dispose pattern.
