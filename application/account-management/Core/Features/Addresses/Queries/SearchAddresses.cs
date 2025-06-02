using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Integrations.Geoapify;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.AccountManagement.Features.Addresses.Queries;

[PublicAPI]
public sealed record SearchAddressesQuery(string? Query = null, string? CountryCode = null) : IRequest<Result<SearchAddressesResponse>>;

[PublicAPI]
public sealed record SearchAddressesResponse(AddressSuggestion[] Suggestions, ServiceStatus ServiceStatus = ServiceStatus.Available, string? ServiceMessage = null);

[PublicAPI]
public enum ServiceStatus
{
    Available,
    NotConfigured,
    NotResponding
}

[PublicAPI]
public sealed record AddressSuggestion(
    string FormattedAddress,
    string? Street,
    string? City,
    string? State,
    string? Zip,
    string? Country
);

public sealed class SearchAddressesQueryValidator : AbstractValidator<SearchAddressesQuery>
{
    public SearchAddressesQueryValidator()
    {
        RuleFor(x => x.Query)
            .MaximumLength(200)
            .WithMessage("Search query must be no longer than 200 characters.");
    }
}

public sealed class SearchAddressesHandler(IGeoapifyClient geoapifyClient) : IRequestHandler<SearchAddressesQuery, Result<SearchAddressesResponse>>
{
    public async Task<Result<SearchAddressesResponse>> Handle(SearchAddressesQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return new SearchAddressesResponse([]);
        }

        var result = await geoapifyClient.SearchAddressesAsync(query.Query, query.CountryCode, cancellationToken);

        if (result.Response is null)
        {
            return new SearchAddressesResponse([], result.ServiceStatus, result.ServiceMessage);
        }

        var suggestions = result.Response.Results.Select(feature => new AddressSuggestion(
                feature.Formatted,
                feature.AddressLine1,
                feature.City,
                feature.State,
                feature.Postcode,
                feature.Country
            )).ToArray();

        return new SearchAddressesResponse(suggestions, result.ServiceStatus, result.ServiceMessage);
    }
}
