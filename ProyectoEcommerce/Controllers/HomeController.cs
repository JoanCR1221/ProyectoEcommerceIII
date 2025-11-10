using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Models;
using ProyectoEcommerce.Data; //  DbContext namespace
using Microsoft.EntityFrameworkCore;

namespace ProyectoEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ProyectoEcommerceContext _context;

        public HomeController(ILogger<HomeController> logger, ProyectoEcommerceContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: Home/Index
        public async Task<IActionResult> Index()
        {
            // Mouestra 9 productos destacados, disponibles y con stock
            var productosDestacados = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Available && p.Stock > 0 && p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt) // más recientes primero
                .Take(9)
                .ToListAsync();

            return View(productosDestacados); // la vista recibirá IEnumerable<Product>
        }


        public IActionResult Privacy() => View();
        public IActionResult LegalInformation() => View();
        public IActionResult SocialMedia() => View();


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
