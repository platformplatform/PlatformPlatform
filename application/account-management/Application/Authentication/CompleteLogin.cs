using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.AccountManagement.Domain.Authentication;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed record CompleteLoginCommand(string OneTimePassword)
    : ICommand, IRequest<Result>
{
    [JsonIgnore]
    public LoginId Id { get; init; } = null!;
}

public sealed class CompleteLoginHandler(
    IUserRepository userRepository,
    ILoginRepository loginProcessRepository,
    IPasswordHasher<object> passwordHasher,
    ITelemetryEventsCollector events,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CompleteLoginHandler> logger
) : IRequestHandler<CompleteLoginCommand, Result>
{
    // TODO: Change this to use ASP.NET data protection API and revert this commit from history before merging
    private static readonly byte[] Key = "q30:l_A}Ubc!UuY@ELE2)^H80Uc:z478'44Llfp!84T^*7NM1Hz478'44Llfp!84T^*7NM1H"u8.ToArray();

    public async Task<Result> Handle(CompleteLoginCommand command, CancellationToken cancellationToken)
    {
        var loginProcess = await loginProcessRepository.GetByIdAsync(command.Id, cancellationToken);

        if (loginProcess is null)
        {
            return Result.NotFound($"Login with id '{command.Id}' not found.");
        }

        if (passwordHasher.VerifyHashedPassword(this, loginProcess.OneTimePasswordHash, command.OneTimePassword)
            == PasswordVerificationResult.Failed)
        {
            loginProcess.RegisterInvalidPasswordAttempt();
            loginProcessRepository.Update(loginProcess);
            events.CollectEvent(new LoginFailed(loginProcess.RetryCount));
            return Result.BadRequest("The code is wrong or no longer valid.", true);
        }

        if (loginProcess.Completed)
        {
            logger.LogWarning(
                "Login with id '{LoginId}' has already been completed.", loginProcess.Id
            );
            return Result.BadRequest(
                $"The login process {loginProcess.Id} for user {loginProcess.UserId} has already been completed."
            );
        }

        if (loginProcess.RetryCount >= Login.MaxAttempts)
        {
            events.CollectEvent(new LoginBlocked(loginProcess.RetryCount));
            return Result.Forbidden("To many attempts, please request a new code.", true);
        }

        var registrationTimeInSeconds = (TimeProvider.System.GetUtcNow() - loginProcess.CreatedAt).TotalSeconds;
        if (loginProcess.HasExpired())
        {
            events.CollectEvent(new LoginExpired((int)registrationTimeInSeconds));
            return Result.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        var user = (await userRepository.GetByIdAsync(loginProcess.UserId, cancellationToken))!;

        loginProcess.MarkAsCompleted();
        loginProcessRepository.Update(loginProcess);

        CreateAuthenticationTokens(user);

        events.CollectEvent(new LoginCompleted(user.Id, (int)registrationTimeInSeconds));

        return Result.Success();
    }

    private void CreateAuthenticationTokens(User user)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        const string issuer = "https://localhost:9000";
        const string audience = "https://localhost:9000";

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName ?? string.Empty),
                    new Claim("role", user.Role.ToString()),
                    new Claim("tenantId", user.TenantId),
                    new Claim("locale", "en"),
                    new Claim("picture", user.Avatar.Url ?? string.Empty)
                }
            ),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Key), SecurityAlgorithms.HmacSha512Signature)
        };

        var refreshToken = new RefreshToken { UserId = user.Id };
        httpContext.Response.Headers.Remove(RefreshToken.XRefreshTokenKey);
        httpContext.Response.Headers.Append(RefreshToken.XRefreshTokenKey, JsonSerializer.Serialize(refreshToken));

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(securityToken);

        httpContext.Response.Headers.Remove(RefreshToken.XAccessTokenKey);
        httpContext.Response.Headers.Append(RefreshToken.XAccessTokenKey, accessToken);
    }
}
