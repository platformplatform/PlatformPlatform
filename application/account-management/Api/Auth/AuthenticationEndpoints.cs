using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Api.Auth.JwtCookieAuthentication;
using IdentityUser = PlatformPlatform.AccountManagement.Infrastructure.Identity.IdentityUser;

namespace PlatformPlatform.AccountManagement.Api.Auth;

public static class AuthenticationEndpoints
{
    private const string RoutesPrefix = "/api/auth";

    public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);

        group.MapPost("/login", async (LoginCommand command, SignInManager<IdentityUser> signInManager) =>
        {
            signInManager.AuthenticationScheme = JwtCookieAuthenticationOptions.DefaultScheme;
            var result = await signInManager.PasswordSignInAsync(command.Email, command.Password, false, true);
            return result.Succeeded ? Results.Ok() : Results.Unauthorized();
        });

        group.MapPost("/logout", async (SignInManager<IdentityUser> signInManager) =>
        {
            signInManager.AuthenticationScheme = JwtCookieAuthenticationOptions.DefaultScheme;
            await signInManager.SignOutAsync();
            return Results.Ok();
        });
    }

    public static void MapPasswordEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);

        group.MapPost("/forgot-password", async (
            ForgotPasswordCommand command,
            UserManager<IdentityUser> userManager,
            IEmailSender<IdentityUser> emailSender
        ) =>
        {
            var user = await userManager.FindByEmailAsync(command.Email);
            if (user is null) return Results.Unauthorized();

            await SendResetPasswordMail(user, userManager, emailSender);
            return Results.Ok();
        });

        group.MapPost("/reset-password", async (ResetPasswordCommand command, UserManager<IdentityUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(command.Email);
            if (user is null) return Results.Unauthorized();

            var result = await userManager.ResetPasswordAsync(user, command.Code, command.Password);
            return result.Succeeded ? Results.Ok() : Results.Unauthorized();
        });

        group.MapPost("/change-password", async (
            ChangePasswordCommand command,
            UserManager<IdentityUser> userManager
        ) =>
        {
            var user = await userManager.FindByEmailAsync(command.Email);
            if (user is null) return Results.Unauthorized();

            var result = await userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
            return result.Succeeded ? Results.Ok() : Results.Unauthorized();
        });
    }

    private static async Task SendResetPasswordMail(
        IdentityUser user,
        UserManager<IdentityUser> userManager,
        IEmailSender<IdentityUser> emailSender
    )
    {
        var passwordResetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailSender.SendPasswordResetCodeAsync(user, user.Email!, passwordResetToken);
    }
}

[UsedImplicitly]
public record LoginCommand(string Email, string Password);

[UsedImplicitly]
public record ForgotPasswordCommand(string Email);

[UsedImplicitly]
public record ResetPasswordCommand(string Email, string Password, string Code);

[UsedImplicitly]
public record ChangePasswordCommand(string Email, string CurrentPassword, string NewPassword);