using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ILLVentApp.Controllers
{
    [Route("AdminView")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AdminViewController : Controller
    {
        private readonly ILogger<AdminViewController> _logger;

        public AdminViewController(ILogger<AdminViewController> logger)
        {
            _logger = logger;
        }

        // GET: /AdminView/Login
        [HttpGet("Login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            ViewData["Title"] = "Admin Login";
            return View("~/Views/Admin/Login.cshtml");
        }

        // GET: /AdminView/Dashboard
        [HttpGet("Dashboard")]
        [AllowAnonymous]
        public IActionResult Dashboard()
        {
            // Client-side JavaScript will handle authentication checks
            ViewData["Title"] = "Admin Dashboard";
            return View("~/Views/Admin/Dashboard.cshtml");
        }

        // GET: /AdminView/Products
        [HttpGet("Products")]
        [AllowAnonymous]
        public IActionResult Products()
        {
            ViewData["Title"] = "Product Management";
            return View("~/Views/Admin/Products.cshtml");
        }

        // GET: /AdminView/Users
        [HttpGet("Users")]
        [AllowAnonymous]
        public IActionResult Users()
        {
            ViewData["Title"] = "User Management";
            return View("~/Views/Admin/Users.cshtml");
        }

        // GET: /AdminView/Hospitals
        [HttpGet("Hospitals")]
        [AllowAnonymous]
        public IActionResult Hospitals()
        {
            ViewData["Title"] = "Hospital Management";
            return View("~/Views/Admin/Hospitals.cshtml");
        }

        // GET: /AdminView/Pharmacies
        [HttpGet("Pharmacies")]
        [AllowAnonymous]
        public IActionResult Pharmacies()
        {
            ViewData["Title"] = "Pharmacy Management";
            return View("~/Views/Admin/Pharmacies.cshtml");
        }

        // GET: /AdminView/Logs
        [HttpGet("Logs")]
        [AllowAnonymous]
        public IActionResult Logs()
        {
            ViewData["Title"] = "System Logs";
            return View("~/Views/Admin/Logs.cshtml");
        }

        // Logout is handled client-side by clearing localStorage

        // GET: /AdminView/
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            return RedirectToAction("Login");
        }
    }
} 