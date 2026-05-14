using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace Lab6.Tests;

public sealed class JwtAuthTests
{
    private const string JwtIssuer = "TicketSales.Auth";
    private const string JwtAudience = "TicketSales.Api";
    private const string JwtKey = "CHANGE_ME_SUPER_SECRET_KEY_32_CHARS_MIN";

    [Fact]
    public async Task TicketSales_Protected_WithoutToken_Returns401()
    {
        await using var factory = new WebApplicationFactory<TicketSalesAPI.Program>();
        var client = factory.CreateClient();

        var resp = await client.PostAsync("/api/auth-test/protected", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task UserService_Protected_WithoutToken_Returns401()
    {
        await using var factory = new WebApplicationFactory<UserService.Program>();
        var client = factory.CreateClient();

        var resp = await client.PostAsync("/api/auth-test/protected", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Protected_WithValidToken_Returns200()
    {
        var token = await GetTokenFromAuthService();

        await using var factory = new WebApplicationFactory<TicketSalesAPI.Program>();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsync("/api/auth-test/protected", content: null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Protected_WithFakeToken_Returns401()
    {
        await using var factory = new WebApplicationFactory<TicketSalesAPI.Program>();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "this.is.not.a.jwt");

        var resp = await client.PostAsync("/api/auth-test/protected", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Protected_WithExpiredToken_Returns401()
    {
        var expired = CreateToken(expiresUtc: DateTime.UtcNow.AddMinutes(-1));

        await using var factory = new WebApplicationFactory<TicketSalesAPI.Program>();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);

        var resp = await client.PostAsync("/api/auth-test/protected", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    private static string CreateToken(DateTime expiresUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "admin"),
            new(JwtRegisteredClaimNames.UniqueName, "admin"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-2),
            expires: expiresUtc,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<string> GetTokenFromAuthService()
    {
        await using var factory = new WebApplicationFactory<AuthService.Program>();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "admin123" });
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        return payload.AccessToken;
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
    }
}