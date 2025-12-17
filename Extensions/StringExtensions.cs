using Microsoft.AspNetCore.Mvc;

namespace NFSE_ABRASF.Extensions
{
    public class StringExtensions : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
