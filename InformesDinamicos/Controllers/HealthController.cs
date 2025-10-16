using Microsoft.AspNetCore.Mvc;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "OK", timestamp = DateTime.UtcNow });
        }
    }
}