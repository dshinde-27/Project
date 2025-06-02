using Microsoft.AspNetCore.Mvc;

namespace CRM_Api.Controllers
{
    public class AuthenticationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
