using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AuthService.Metrics;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtTokenService _jwt;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(JwtTokenService jwt, IHttpClientFactory httpClientFactory)
    {
        _jwt = jwt;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("login")]
    public async Task<ActionResult<object>> Login([FromBody] LoginRequest request)
    {
        var client = _httpClientFactory.CreateClient("UserService");
        var response = await client.PostAsJsonAsync("api/users/validate", new
        {
            username = request.Username,
            password = request.Password
        });

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            AuthMetrics.LoginAttempts.WithLabels(AuthMetrics.ResultInvalidCredentials).Inc();
            return Unauthorized(new { message = "Неверное имя пользователя или пароль" });
        }

        if (!response.IsSuccessStatusCode)
        {
            AuthMetrics.LoginAttempts.WithLabels(AuthMetrics.ResultUserServiceUnavailable).Inc();
            return StatusCode((int)response.StatusCode, new { message = "Сервис пользователей недоступен" });
        }

        var user = await response.Content.ReadFromJsonAsync<ValidatedUserResponse>(new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (user == null || string.IsNullOrWhiteSpace(user.UserId))
        {
            AuthMetrics.LoginAttempts.WithLabels(AuthMetrics.ResultInvalidCredentials).Inc();
            return Unauthorized(new { message = "Неверное имя пользователя или пароль" });
        }

        var token = _jwt.CreateAccessToken(user.UserId, user.UserName ?? request.Username);
        AuthMetrics.LoginAttempts.WithLabels(AuthMetrics.ResultSuccess).Inc();
        AuthMetrics.TokensIssued.Inc();
        return Ok(new { accessToken = token, tokenType = "Bearer", userId = user.UserId, userName = user.UserName });
    }

    private sealed class ValidatedUserResponse
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("userName")]
        public string? UserName { get; set; }
    }
}
