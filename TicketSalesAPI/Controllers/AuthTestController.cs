using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketSalesAPI.Controllers;

[ApiController]
[Route("api/auth-test")]
public sealed class AuthTestController : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    public ActionResult<object> Public() => Ok(new { ok = true, auth = "none" });

    [HttpPost("protected")]
    [Authorize]
    public ActionResult<object> Protected()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Ok(new { ok = true, sub });
    }
}

