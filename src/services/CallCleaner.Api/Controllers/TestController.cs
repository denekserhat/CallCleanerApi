using Microsoft.AspNetCore.Mvc;

namespace CallCleaner.Api.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return Ok("TEST");
        }
    }
}
