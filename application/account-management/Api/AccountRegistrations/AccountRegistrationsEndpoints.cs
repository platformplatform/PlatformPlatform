using PlatformPlatform.AccountManagement.Application.AccountRegistrations;
using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;

namespace PlatformPlatform.AccountManagement.Api.AccountRegistrations;

public class AccountRegistrationsEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-registrations";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);

        group.MapGet("/is-subdomain-free",
            async Task<ApiResult<bool>> ([AsParameters] IsSubdomainFreeQuery query, ISender mediator)
                => await mediator.Send(query));

        group.MapPost("/start", async Task<ApiResult> (StartAccountRegistrationCommand command, ISender mediator)
            => (await mediator.Send(command)).AddResourceUri(RoutesPrefix));

        group.MapPost("{id}/complete", async Task<ApiResult> (
                AccountRegistrationId id,
                CompleteAccountRegistrationCommand command,
                ISender mediator
            )
            => await mediator.Send(command with { Id = id }));
    }
}