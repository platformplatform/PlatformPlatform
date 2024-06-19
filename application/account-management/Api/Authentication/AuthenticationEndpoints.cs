using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Authentication;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Api.Authentication;

public class AuthenticationEndpoints : IEndpoints
{
    private const string Issuer = "https://localhost:9000";
    private const string Audience = "https://localhost:9000";

    private const string RoutesPrefix = "/api/account-management/authentication";
    private const string XAccessTokenKey = "X-Access-Token";
    private const string XRefreshTokenKey = "X-Refresh-Token";
    private static readonly byte[] Key = "q30:l_A}Ubc!UuY@ELE2)^H80Uc:z478'44Llfp!84T^*7NM1Hz478'44Llfp!84T^*7NM1H"u8.ToArray();

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Authentication");

        group.MapPost("/{email}", async Task<ApiResult> (HttpContext http, IUserRepository userRepository, string email) =>
            {
                try
                {
                    http.Request.Headers.TryGetValue(XRefreshTokenKey, out var refreshTokens);
                    http.Request.Headers.TryGetValue(XAccessTokenKey, out var accessToken);

                    var searchResult = await userRepository.Search(email, null, null, null, 1, 0, new CancellationToken());
                    var user = searchResult.Users.FirstOrDefault();

                    if (user is null) return Result.Unauthorized("User not found");

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new[]
                            {
                                new Claim("Id", Guid.NewGuid().ToString()),
                                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                                new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName ?? string.Empty),
                                new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName ?? string.Empty)
                            }
                        ),
                        Expires = DateTime.UtcNow.AddMinutes(5),
                        Issuer = Issuer,
                        Audience = Audience,
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Key), SecurityAlgorithms.HmacSha512Signature)
                    };

                    var tokenHandler = new JwtSecurityTokenHandler();
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    accessToken = tokenHandler.WriteToken(token);

                    RefreshToken refreshToken;
                    var existingRefreshTokenJson = refreshTokens.Count == 1 ? refreshTokens[0] : null;
                    if (existingRefreshTokenJson is not null)
                    {
                        refreshToken = JsonSerializer.Deserialize<RefreshToken>(existingRefreshTokenJson)!;
                        refreshToken.Version++;
                    }
                    else
                    {
                        refreshToken = new RefreshToken();
                    }

                    http.Response.Headers.Remove(XRefreshTokenKey);
                    http.Response.Headers.Append(XRefreshTokenKey, JsonSerializer.Serialize(refreshToken));

                    http.Response.Headers.Remove(XAccessTokenKey);
                    http.Response.Headers.Append(XAccessTokenKey, accessToken);

                    return Result.Success();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return Result.BadRequest(ex.Message);
                }
            }
        );
    }
}
