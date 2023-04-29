using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants.commands;

public class CreateTenantCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository;
    private readonly CreateTenantCommandValidator _validator;

    public CreateTenantCommandHandlerTests()
    {
        _tenantRepository = Substitute.For<ITenantRepository>();
        _validator = new CreateTenantCommandValidator(_tenantRepository);
    }

    [Fact]
    public async Task CreateTenantCommandHandler_ShouldAddTenantToRepository()
    {
        // Arrange
        var startId = TenantId.NewId(); // NewId will always generate an id that are greater than the previous one
        var tenantRepository = Substitute.For<ITenantRepository>();
        var handler = new CreateTenantCommandHandler(tenantRepository);

        // Act
        var command = new CreateTenantCommand("TestTenant", "tenant1", "foo@tenant1.com", "1234567890");
        var tenantResponseDto = await handler.Handle(command, CancellationToken.None);

        // Assert
        var tenantId = TenantId.FromString(tenantResponseDto.Id);
        await tenantRepository.Received()
            .AddAsync(Arg.Is<Tenant>(t => t.Name == command.Name && t.Id > startId && t.Id == tenantId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTenantCommandHandler_ShouldReturnTenantDtoWithCorrectValues()
    {
        // Arrange
        var tenantRepository = Substitute.For<ITenantRepository>();
        var handler = new CreateTenantCommandHandler(tenantRepository);

        // Act
        var command = new CreateTenantCommand("TestTenant", "tenant1", "foo@tenant1.com", "1234567890");
        var tenantResponseDto = await handler.Handle(command, CancellationToken.None);

        // Assert
        tenantResponseDto.Name.Should().Be(command.Name);
        tenantResponseDto.Email.Should().Be(command.Email);
        tenantResponseDto.Phone.Should().Be(command.Phone);
    }

    [Theory]
    [InlineData("Valid tenant", "tenant1", "foo@tenant1.com", "1234567890", true)]
    [InlineData("", "notenantname", "foo@tenant1.com", "1234567890", false)]
    [InlineData("Too long tenant name above 30 characters", "tenant1", "foo@tenant1.com", "1234567890", false)]
    [InlineData("No email", "tenant1", "", "1234567890", false)]
    [InlineData("Invalid Email", "tenant1", "@tenant1.com", "1234567890", false)]
    [InlineData("No phone number", "tenant1", "foo@tenant1.com", "", false)]
    [InlineData("To long phone number", "tenant1", "foo@tenant1.com", "1234567890123456", false)]
    [InlineData("Invalid phone number", "tenant1", "foo@tenant1.com", "N/A", false)]
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

        // Act
        var validationResult = await _validator.ValidateAsync(command, CancellationToken.None);

        // Assert
        validationResult.IsValid.Should().Be(expected);
    }
}