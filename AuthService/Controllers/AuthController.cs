using System.Diagnostics.Metrics;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtTokenService _jwt;

    private const string DemoUsername = "admin";
    private const string DemoPassword = "admin123";
    private const string DemoUserId = "admin";

    private static readonly Counter LoginAttempts = Metrics.CreateCounter(
        "auth_attempts_total",
        "Total login attempts",
        new CounterConfiguration { LabelNames = new[] { "result" } });

    public AuthController(JwtTokenService jwt)
    {
        _jwt = jwt;
    }

    [HttpPost("login")]
    public ActionResult<object> Login([FromBody] LoginRequest request)
    {
        if (!string.Equals(request.Username, DemoUsername, StringComparison.Ordinal) ||
            !string.Equals(request.Password, DemoPassword, StringComparison.Ordinal))
        {
            LoginAttempts.WithLabels("failure").Inc();
            return Unauthorized(new { message = "Invalid credentials" });
        }

        LoginAttempts.WithLabels("success").Inc();
        var token = _jwt.CreateAccessToken(DemoUserId, DemoUsername);
        return Ok(new { accessToken = token, tokenType = "Bearer" });
    }
}