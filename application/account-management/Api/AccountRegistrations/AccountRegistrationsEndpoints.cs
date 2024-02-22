using PlatformPlatform.AccountManagement.Application.AccountRegistrations;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;

namespace PlatformPlatform.AccountManagement.Api.AccountRegistrations;

public static class AccountRegistrationsEndpoints
{
    private const string RoutesPrefix = "/api/account-registrations";

    public static void MapAccountRegistrationsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);

        group.MapPost("/start", async Task<ApiResult> (StartAccountRegistrationCommand command, ISender mediator)
            => (await mediator.Send(command)).AddResourceUri(RoutesPrefix));
    }
}