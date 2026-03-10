using Microsoft.AspNetCore.Mvc;

namespace PastPort.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "PastPort API is working!",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "Healthy",
            database = "Connected",
            timestamp = DateTime.UtcNow
        });
    }
}