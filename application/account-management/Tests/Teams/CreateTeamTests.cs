using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Teams.Commands;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Teams;

public sealed class CreateTeamTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task CreateTeam_WhenValid_ShouldCreateTeam()
    {
        // Arrange
        var command = new CreateTeamCommand(Faker.Company.CompanyName(), Faker.Company.CatchPhrase());

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/teams", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var teamId = await response.Content.ReadFromJsonAsync<TeamId>();
        Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM Teams WHERE Id = @id", new { id = teamId!.ToString() }).Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamCreated");
    }

    [Fact]
    public async Task CreateTeam_WhenNameIsEmpty_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreateTeamCommand(string.Empty, Faker.Company.CatchPhrase());

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/teams", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Name", "Name must be between 1 and 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task CreateTeam_WhenNameIsTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreateTeamCommand(new string('a', 101), Faker.Company.CatchPhrase());

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/teams", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Name", "Name must be between 1 and 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task CreateTeam_WhenNameIsExactly100Characters_ShouldCreateTeam()
    {
        // Arrange
        var command = new CreateTeamCommand(new string('a', 100), Faker.Company.CatchPhrase());

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/teams", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var teamId = await response.Content.ReadFromJsonAsync<TeamId>();
        Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM Teams WHERE Id = @id", new { id = teamId!.ToString() }).Should().Be(1);
    }

    [Fact]
    public async Task CreateTeam_WhenDescriptionIsTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreateTeamCommand(Faker.Company.CompanyName(), new string('a', 501));

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/teams", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Description", "Description must be at most 500 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task CreateTeam_WhenDescriptionIsExactly500Characters_ShouldCreateTeam()
    {
        // Arrange
        var command = new CreateTeamCommand(Faker.Company.CompanyName(), new string('a', 500));

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/teams", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var teamId = await response.Content.ReadFromJsonAsync<TeamId>();
        Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM Teams WHERE Id = @id", new { id = teamId!.ToString() }).Should().Be(1);
    }

    [Fact]
    public async Task CreateTeam_WhenDescriptionIsEmpty_ShouldCreateTeam()
    {
        // Arrange
        var command = new CreateTeamCommand(Faker.Company.CompanyName(), string.Empty);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/teams", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var teamId = await response.Content.ReadFromJsonAsync<TeamId>();
        Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM Teams WHERE Id = @id", new { id = teamId!.ToString() }).Should().Be(1);
    }

    [Fact]
    public async Task CreateTeam_WhenDuplicateNameWithinTenant_ShouldReturnConflict()
    {
        // Arrange
        var teamName = Faker.Company.CompanyName();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", TeamId.NewId().ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", teamName),
                ("Description", Faker.Company.CatchPhrase())
            ]
        );
        var command = new CreateTeamCommand(teamName, Faker.Company.CatchPhrase());

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/teams", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Conflict, $"A team with the name '{teamName}' already exists.");
    }

    [Fact]
    public async Task CreateTeam_WhenNonOwnerUser_ShouldReturnForbidden()
    {
        // Arrange
        var command = new CreateTeamCommand(Faker.Company.CompanyName(), Faker.Company.CatchPhrase());

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account-management/teams", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only tenant owners can create teams.");
    }
}
