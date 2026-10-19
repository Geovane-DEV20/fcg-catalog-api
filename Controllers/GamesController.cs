using Microsoft.AspNetCore.Mvc;

namespace FcgCatalogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Catalog API is running" });
    }
}
