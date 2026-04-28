using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtTokenService _jwt;

    // Минимальный вариант для лабы: один тестовый пользователь.
    // При желании можно заменить на Mongo/UsersService позже.
    private const string DemoUsername = "admin";
    private const string DemoPassword = "admin123";
    private const string DemoUserId = "admin";

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
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var token = _jwt.CreateAccessToken(DemoUserId, DemoUsername);
        return Ok(new { accessToken = token, tokenType = "Bearer" });
    }
}

