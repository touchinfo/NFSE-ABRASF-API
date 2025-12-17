using Microsoft.AspNetCore.Mvc;

namespace NFSE_ABRASF.Data.Configurations
{
    public class EmpresaConfiguration : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
