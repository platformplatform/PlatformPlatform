using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Mapster;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public static class UpdateTenant
{
    public sealed record Command : ICommand, ITenantValidation, IRequest<Result<TenantResponseDto>>
    {
        [JsonIgnore] // Removes the Id from the API contract
        public TenantId Id { get; init; } = null!;

        public required string Name { get; init; }

        public required string Email { get; init; }

        public string? Phone { get; init; }
    }

    public sealed class Handler : IRequestHandler<Command, Result<TenantResponseDto>>
    {
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result<TenantResponseDto>> Handle(Command command, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
            if (tenant is null)
            {
                return Result<TenantResponseDto>.NotFound($"Tenant with id '{command.Id}' not found.");
            }

            tenant.Update(command.Name, command.Email, command.Phone);
            _tenantRepository.Update(tenant);
            return tenant.Adapt<TenantResponseDto>();
        }
    }

    [UsedImplicitly]
    public sealed class Validator : TenantValidator<Command>
    {
    }
}