using Microsoft.AspNetCore.Mvc;
using ILLVentApp.Domain.Models;
using ILLVentApp.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ILLVentApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> TestImages()
        {
            var hospitals = await _context.Set<Hospital>().ToListAsync();
            return View(hospitals);
        }

        public async Task<IActionResult> TestImagesOld()
        {
            return RedirectToAction(nameof(TestImages));
        }
    }
} 