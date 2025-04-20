using Microsoft.AspNetCore.Mvc;

namespace CallCleaner.Api.Controllers
{
    [Produces("application/json")]
    [Consumes("application/json")]
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }
        [HttpGet("get-test")]
        public IActionResult GetTestString()
        {
            _logger.LogInformation("Test endpoint çağrıldı, CloudWatch kontrolü yapılıyor.");
            return Ok("TEST");
        }
    }
}