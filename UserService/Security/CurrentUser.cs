using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace UserService.Security;

public static class CurrentUser
{
    public static string? GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
}
