using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Mizan.Api.Infrastructure;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The access token has no 'sub' claim."));
}
