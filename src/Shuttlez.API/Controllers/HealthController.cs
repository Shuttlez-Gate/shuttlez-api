using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            status = "healthy",
            service = "Shuttlez.API",
            timestamp = DateTime.UtcNow
        }));
    }
}
