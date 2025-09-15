using Microsoft.AspNetCore.Mvc;

namespace SellCatcher.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHello() => Ok(new { message = "Hello from SellCatcher API!" });
}
