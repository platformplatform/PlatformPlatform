namespace PlatformPlatform.AccountManagement.Integrations.Geoapify;

public sealed record GeoapifySearchRequest(string Text, int Limit = 20);

public sealed record GeoapifySearchResponse(
    [property: JsonPropertyName("results")]
    GeoapifyFeature[] Results
);

public sealed record GeoapifyFeature(
    [property: JsonPropertyName("formatted")]
    string Formatted,
    [property: JsonPropertyName("address_line1")]
    string? AddressLine1,
    [property: JsonPropertyName("address_line2")]
    string? AddressLine2,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("postcode")]
    string? Postcode,
    [property: JsonPropertyName("country")]
    string? Country,
    [property: JsonPropertyName("rank")] GeoapifyRank? Rank
);

public sealed record GeoapifyRank(
    [property: JsonPropertyName("confidence")]
    double Confidence
);
