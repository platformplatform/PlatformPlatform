using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Integrations.Geoapify;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.AccountManagement.Features.Addresses.Queries;

[PublicAPI]
public sealed record SearchAddressesQuery(string? Query = null, string? CountryCode = null) : IRequest<Result<SearchAddressesResponse>>;

[PublicAPI]
public sealed record SearchAddressesResponse(AddressSuggestion[] Suggestions);

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

        var response = await geoapifyClient.SearchAddressesAsync(query.Query, query.CountryCode, cancellationToken);

        if (response is null)
        {
            return new SearchAddressesResponse([]);
        }

        var suggestions = response.Results.Select(feature => new AddressSuggestion(
                feature.Formatted,
                feature.AddressLine1,
                feature.City,
                feature.State,
                feature.Postcode,
                feature.Country
            )
        ).ToArray();

        return new SearchAddressesResponse(suggestions);
    }
}
