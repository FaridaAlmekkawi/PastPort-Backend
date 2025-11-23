using Microsoft.AspNetCore.Mvc;

namespace PastPort.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult HandleResult<T>(T? data, string? message = null)
    {
        if (data == null)
            return NotFound(new { message = message ?? "Resource not found" });

        return Ok(new { data, message = message ?? "Success" });
    }

    protected IActionResult HandleError(Exception ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}