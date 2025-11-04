using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;
using ProyectoEcommerce.Services;

namespace ProyectoEcommerce.Controllers
{
    [Authorize] // requiere login por defecto
    public class ShoppingCartsController : Controller
    {
        private readonly ProyectoEcommerceContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<ShoppingCartsController> _logger;
        private const decimal IVA_RATE = 0.13m;

        public ShoppingCartsController(ProyectoEcommerceContext context, ICartService cartService, ILogger<ShoppingCartsController> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        // ========= ADMIN: listado global =========
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var query = _context.ShoppingCarts
                .Include(s => s.Customer)
                .OrderByDescending(s => s.CreatedDate);
            return View(await query.ToListAsync());
        }

        // ========= Mi carrito (usuario autenticado) =========
        public async Task<IActionResult> My()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Challenge();

            var cart = await _cartService.GetCartByEmailAsync(email);
            if (cart == null)
            {
                // el servicio crea el carrito cuando se añade un producto.
                return View(null);
            }

            return View(cart); // Views/ShoppingCarts/My.cshtml
        }

        // Añadir producto al carrito (desde la página de producto)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string returnUrl = null)
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Challenge();

            try
            {
                await _cartService.AddToCartAsync(email, productId, quantity);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("My");
        }

        // Pagar: delega al servicio que hace la transacción
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Pay: usuario no autenticado.");
                return Challenge();
            }

            try
            {
                _logger.LogInformation("Pay iniciado para {Email}", email);
                var buy = await _cartService.CreateBuyFromCartAsync(email, IVA_RATE);
                _logger.LogInformation("Pay completado. BuyId={BuyId} para {Email}", buy.BuyId, email);
                TempData["Success"] = "Pago realizado correctamente.";
                return RedirectToAction("Details", "Buys", new { id = buy.BuyId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Pay falló (operación inválida) para {Email}", email);
                TempData["Error"] = ex.Message;
                return RedirectToAction("My");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pay falló inesperadamente para {Email}", email);
                TempData["Error"] = "Error al procesar el pago. Inténtalo de nuevo más tarde.";
                return RedirectToAction("My");
            }
        }

        // Actualiza la cantidad de un item del carrito
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartId, int productId, int quantity)
        {
            await _cartService.UpdateQuantityAsync(cartId, productId, quantity);
            return RedirectToAction("My");
        }

        // Elimina un item del carrito
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartId, int productId)
        {
            await _cartService.RemoveItemAsync(cartId, productId);
            return RedirectToAction("My");
        }

        // ========= Details: Admin ve todo; dueño puede ver el suyo =========
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cart = await _context.ShoppingCarts
                .Include(s => s.Customer)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(m => m.ShoppingCartId == id);
            if (cart == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var email = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(email) ||
                    !string.Equals(cart.Customer?.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            return View(cart);
        }

        // ========= CRUD ADMIN =========
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Customers, "CustomerId", "Email");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("ShoppingCartId,CreatedDate,CustomerId")] ShoppingCart shoppingCart)
        {
            if (!ModelState.IsValid)
            {
                ViewData["CustomerId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Customers, "CustomerId", "Email", shoppingCart.CustomerId);
                return View(shoppingCart);
            }

            _context.Add(shoppingCart);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var shoppingCart = await _context.ShoppingCarts.FindAsync(id);
            if (shoppingCart == null) return NotFound();

            ViewData["CustomerId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Customers, "CustomerId", "Email", shoppingCart.CustomerId);
            return View(shoppingCart);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("ShoppingCartId,CreatedDate,CustomerId")] ShoppingCart shoppingCart)
        {
            if (id != shoppingCart.ShoppingCartId) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewData["CustomerId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Customers, "CustomerId", "Email", shoppingCart.CustomerId);
                return View(shoppingCart);
            }

            try
            {
                _context.Update(shoppingCart);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShoppingCartExists(shoppingCart.ShoppingCartId)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var shoppingCart = await _context.ShoppingCarts
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(m => m.ShoppingCartId == id);
            if (shoppingCart == null) return NotFound();

            return View(shoppingCart);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shoppingCart = await _context.ShoppingCarts.FindAsync(id);
            if (shoppingCart != null)
            {
                _context.ShoppingCarts.Remove(shoppingCart);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShoppingCartExists(int id)
            => _context.ShoppingCarts.Any(e => e.ShoppingCartId == id);
    }
}