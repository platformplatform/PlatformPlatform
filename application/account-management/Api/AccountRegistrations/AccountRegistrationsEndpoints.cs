using PlatformPlatform.AccountManagement.Application.AccountRegistrations;
using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.AccountRegistrations;

public class AccountRegistrationsEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/account-registrations";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("AccountRegistrations");

        group.MapGet("/is-subdomain-free", async Task<ApiResult<bool>> ([AsParameters] IsSubdomainFreeQuery query, ISender mediator)
            => await mediator.Send(query)
        ).Produces<bool>();

        group.MapPost("/start", async Task<ApiResult<StartAccountRegistrationResponse>> (StartAccountRegistrationCommand command, ISender mediator)
            => await mediator.Send(command)
        ).Produces<StartAccountRegistrationResponse>();

        group.MapPost("{id}/complete", async Task<ApiResult> (AccountRegistrationId id, CompleteAccountRegistrationCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id })
        );
    }
}
