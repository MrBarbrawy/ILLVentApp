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

        public async Task<IActionResult> TestDoctorImages()
        {
            var doctors = await _context.Set<Doctor>().ToListAsync();
            return View(doctors);
        }

        public async Task<IActionResult> TestPharmacyImages()
        {
            var pharmacies = await _context.Set<Pharmacy>().ToListAsync();
            return View(pharmacies);
        }

        public async Task<IActionResult> TestImagesOld()
        {
            return RedirectToAction(nameof(TestImages));
        }
    }
} 