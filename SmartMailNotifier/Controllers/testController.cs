using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartMailNotifier.Controllers
{
    public class testController : ControllerBase
    {
            [Authorize]
            [HttpGet("test")]
            public IActionResult Test()
            {
                return Ok("secure API is working!");
        }
    }
}
