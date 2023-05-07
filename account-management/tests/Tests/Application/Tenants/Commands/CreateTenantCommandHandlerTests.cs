using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands.CreateTenant;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants.Commands.CreateTenant;

public class CreateTenantCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository;

    public CreateTenantCommandHandlerTests()
    {
        _tenantRepository = Substitute.For<ITenantRepository>();
    }

    [Fact]
    public async Task CreateTenantCommandHandler_ShouldAddTenantToRepository()
    {
        // Arrange
        var startId = TenantId.NewId(); // NewId will always generate an id that are greater than the previous one
        _tenantRepository.IsSubdomainFreeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new CreateTenantCommandHandler(_tenantRepository);

        // Act
        var command = new CreateTenantCommand("TestTenant", "tenant1", "foo@tenant1.com", "1234567890");
        var createTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        createTenantCommandResult.IsSuccess.Should().BeTrue();
        var tenantResponseDto = createTenantCommandResult.Value;
        var tenantId = TenantId.FromString(tenantResponseDto.Id);
        await _tenantRepository.Received()
            .AddAsync(Arg.Is<Tenant>(t => t.Name == command.Name && t.Id > startId && t.Id == tenantId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTenantCommandHandler_ShouldReturnTenantDtoWithCorrectValues()
    {
        // Arrange
        _tenantRepository.IsSubdomainFreeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new CreateTenantCommandHandler(_tenantRepository);

        // Act
        var command = new CreateTenantCommand("TestTenant", "tenant1", "foo@tenant1.com", "1234567890");
        var createTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        createTenantCommandResult.IsSuccess.Should().BeTrue();
        var tenantResponseDto = createTenantCommandResult.Value;
        tenantResponseDto.Name.Should().Be(command.Name);
        tenantResponseDto.Email.Should().Be(command.Email);
        tenantResponseDto.Phone.Should().Be(command.Phone);
    }

    [Theory]
    [InlineData("Valid properties", "tenant1", "foo@tenant1.com", "+44 (0)20 7946 0123", true)]
    [InlineData("No phone number (valid)", "tenant1", "foo@tenant1.com", null, true)]
    [InlineData("Empty phone number", "tenant1", "foo@tenant1.com", "", true)]
    [InlineData("To long phone number", "tenant1", "foo@tenant1.com", "0099 (999) 888-77-66-55", false)]
    [InlineData("Invalid phone number", "tenant1", "foo@tenant1.com", "N/A", false)]
    [InlineData("", "notenantname", "foo@tenant1.com", "1234567890", false)]
    [InlineData("Too long tenant name above 30 characters", "tenant1", "foo@tenant1.com", "+55 (21) 99999-9999", false)]
    [InlineData("No email", "tenant1", "", "+61 2 1234 5678", false)]
    [InlineData("Invalid Email", "tenant1", "@tenant1.com", "1234567890", false)]
    [InlineData("No subdomain", "", "foo@tenant1.com", "1234567890", false)]
    [InlineData("To short subdomain", "ab", "foo@tenant1.com", "1234567890", false)]
    [InlineData("To long subdomain", "1234567890123456789012345678901", "foo@tenant1.com", "1234567890", false)]
    [InlineData("Subdomain with uppercase", "Tenant1", "foo@tenant1.com", "1234567890", false)]
    [InlineData("Subdomain special characters", "tenant-1", "foo@tenant1.com", "1234567890", false)]
    [InlineData("Subdomain with spaces", "tenant 1", "foo@tenant1.com", "1234567890", false)]
    public async Task CreateTenantCommandValidator_ShouldValidateCorrectly(string name, string subdomain, string email,
        string phone, bool expected)
    {
        // Arrange
        var command = new CreateTenantCommand(name, subdomain, email, phone);
        _tenantRepository.IsSubdomainFreeAsync(subdomain, Arg.Any<CancellationToken>()).Returns(true);
        var createTenantCommandHandler = new CreateTenantCommandHandler(_tenantRepository);

        // Act
        var commandResult = await createTenantCommandHandler.Handle(command, CancellationToken.None);

        // Assert
        commandResult.IsSuccess.Should().Be(expected);
        commandResult.Errors.Length.Should().Be(expected ? 0 : 1);
    }
}