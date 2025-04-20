using Microsoft.AspNetCore.Mvc;

namespace CallCleaner.Api.Controllers
{
    [Produces("application/json")]
    [Consumes("application/json")]
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        [HttpGet("get-test")]
        public IActionResult GetTestString()
        {
            return Ok("TEST");
        }
    }
}