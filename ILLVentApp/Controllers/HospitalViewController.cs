using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ILLVentApp.Controllers
{
    public class HospitalViewController : Controller
    {
        private readonly ILogger<HospitalViewController> _logger;

        public HospitalViewController(ILogger<HospitalViewController> logger)
        {
            _logger = logger;
        }

        // GET: /HospitalView/Login
        [AllowAnonymous]
        public IActionResult Login()
        {
            ViewData["Title"] = "Hospital Staff Login";
            return View("~/Views/HospitalDashboard/Login.cshtml");
        }

        // GET: /HospitalView/Dashboard
        // Note: Authorization is handled client-side via JavaScript
        // since JWT tokens are stored in localStorage
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Hospital Emergency Dashboard";
            return View("~/Views/HospitalDashboard/Index.cshtml");
        }

        // GET: /HospitalView/
        public IActionResult Index()
        {
            return RedirectToAction("Login");
        }
    }
} 