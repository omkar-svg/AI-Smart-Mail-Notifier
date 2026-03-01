using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMailNotifier.Models;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("[controller]")]
public class CheckUserController : ControllerBase
{
    [HttpGet("check")]
    public IActionResult CheckUser()
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new
        {
            message = "Authorized",
            userId = userId
        });
    }
}