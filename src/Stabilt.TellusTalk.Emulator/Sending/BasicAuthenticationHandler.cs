using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Stabilt.TellusTalk.Emulator.Ids;

namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>TellusTalk's HTTP Basic authentication as the emulator plays it: any user name and password, but sent up front.</summary>
internal sealed class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TimeProvider time) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Basic";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (BasicCredentials.UserName(Request.Headers.Authorization) is not { } userName)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, userName)], Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }

    /// <summary>TellusTalk's 401 carries the headers of its every answer and its own text, but no WWW-Authenticate.</summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        TellusTalkHeaders.Apply(Response, Guid.CreateVersion1(time.GetUtcNow()).ToString("N"));

        return Refusals.Unauthorized(Response);
    }
}
