

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;

namespace ProyectoEcommerce.Controllers
{
    [Authorize] // requiere login por defecto
    public class ShoppingCartsController : Controller
    {
        private readonly ProyectoEcommerceContext _context;
        private const decimal IVA_RATE = 0.13m; // ajusta si necesitas otro porcentaje

        public ShoppingCartsController(ProyectoEcommerceContext context)
        {
            _context = context;
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

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null)
            {
                return RedirectToAction("My", "Customers");
            }

            var cart = await _context.ShoppingCarts
                .Include(s => s.Customer)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.CustomerId == customer.CustomerId);

            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    CustomerId = customer.CustomerId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return View(cart); // Views/ShoppingCarts/My.cshtml
        }

        // Añadir producto al carrito (desde la página de producto)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string returnUrl = null)
        {
            if (quantity <= 0) quantity = 1;

            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Challenge();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null) return RedirectToAction("My", "Customers");

            var cart = await _context.ShoppingCarts
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.CustomerId == customer.CustomerId);

            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    CustomerId = customer.CustomerId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Intentar obtener el producto (para precio/validaciones opcionales)
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var existingItem = await _context.ShoppingCartItems
                .FirstOrDefaultAsync(i => i.ShoppingCartId == cart.ShoppingCartId && i.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                _context.ShoppingCartItems.Update(existingItem);
            }
            else
            {
                var item = new ShoppingCartItem
                {
                    ShoppingCartId = cart.ShoppingCartId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedDate = DateTime.UtcNow
                };
                _context.ShoppingCartItems.Add(item);
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("My");
        }

        // Pagar: crea un Buy y sus BuyItems, elimina el ShoppingCart y redirige a la "factura" (Buy Details)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Challenge();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null) return RedirectToAction("My", "Customers");

            var cart = await _context.ShoppingCarts
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.CustomerId == customer.CustomerId);

            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction("My");
            }

            // Calcular importes
            decimal subtotal = cart.Items.Sum(i => i.Quantity * i.Product.Price);
            decimal iva = Math.Round(subtotal * IVA_RATE, 2);
            decimal total = subtotal + iva;

            // Crear Buy
            var buy = new Buy
            {
                CustomerId = customer.CustomerId,
                EmployeeId = _context.Employees.Select(e => (int?)e.EmployeeId).FirstOrDefault(), // si no hay empleados queda null
                Fecha = DateTime.UtcNow,
                Subtotal = subtotal,
                IVA = iva,
                Total = total,
                Paid = true
            };

            // Añadir Buy y BuyItems
            _context.Buys.Add(buy);
            // Guardar para obtener BuyId si es necesario (EF maneja relación aunque no guardemos inmediatamente)
            await _context.SaveChangesAsync();

            foreach (var ci in cart.Items)
            {
                var bi = new BuyItem
                {
                    BuyId = buy.BuyId,
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price,
                    Subtotal = ci.Quantity * ci.Product.Price
                };
                _context.BuyItems.Add(bi);

                // Opcional: reducir stock del producto
                var prod = await _context.Products.FindAsync(ci.ProductId);
                if (prod != null)
                {
                    prod.Stock = Math.Max(0, prod.Stock - ci.Quantity);
                    _context.Products.Update(prod);
                }
            }

            // Eliminar carrito (cascade eliminará ShoppingCartItems)
            _context.ShoppingCarts.Remove(cart);

            await _context.SaveChangesAsync();

            // Redirigir a factura (Detalles de la compra)
            return RedirectToAction("Details", "Buys", new { id = buy.BuyId });
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