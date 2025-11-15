using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;
using ProyectoEcommerce.Services;
using Microsoft.AspNetCore.Http;

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

            // Obtener cupón aplicado (si existe) en sesión y validar su vigencia
            var couponCode = HttpContext.Session.GetString("CurrentCouponCode");
            decimal? couponPercent = null;
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);

                if (coupon != null)
                {
                    var today = DateTime.Today;
                    if (today >= coupon.ValidFrom.Date && today <= coupon.ValidTo.Date)
                    {
                        couponPercent = coupon.DiscountPercent;
                    }
                    else
                    {
                        // cupón expirado -> limpiar sesión
                        HttpContext.Session.Remove("CurrentCouponCode");
                        couponCode = null;
                    }
                }
                else
                {
                    // cupón inválido -> limpiar sesión
                    HttpContext.Session.Remove("CurrentCouponCode");
                    couponCode = null;
                }
            }

            ViewData["CurrentCouponCode"] = couponCode;
            ViewData["CurrentCouponPercent"] = couponPercent;

            return View(cart); // Views/ShoppingCarts/My.cshtml
        }

        // Añadir producto al carrito (desde la página de producto)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string returnUrl = null)
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
            {
                // Redirigir explícitamente al login con returnUrl
                return RedirectToPage("/Account/Login", new
                {
                    area = "Identity",
                    returnUrl = returnUrl ?? Url.Action("DetailsPublic", "Products", new { id = productId })
                });
            }

            try
            {
                await _cartService.AddToCartAsync(email, productId, quantity);
                TempData["Success"] = "Producto agregado al carrito exitosamente";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            // Redirigir de vuelta a la página del producto
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("DetailsPublic", "Products", new { id = productId });
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

                // Primero crear la compra normal (sin cupón)
                var buy = await _cartService.CreateBuyFromCartAsync(email, IVA_RATE);

                // Ver si hay cupón en sesión
                var couponCode = HttpContext.Session.GetString("CurrentCouponCode");
                if (!string.IsNullOrWhiteSpace(couponCode))
                {
                    var coupon = await _context.Coupons
                        .FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);

                    if (coupon != null)
                    {
                        // Recalcular descuento basado en el subtotal de la compra
                        var buyFromDb = await _context.Buys
                            .Include(b => b.Items)
                            .FirstOrDefaultAsync(b => b.BuyId == buy.BuyId);

                        if (buyFromDb != null)
                        {
                            var subtotal = buyFromDb.Subtotal;

                            var discount = subtotal * (coupon.DiscountPercent / 100m);

                            if (discount > subtotal)
                                discount = subtotal;

                            buyFromDb.CouponCode = coupon.Code;
                            buyFromDb.DiscountAmount = discount;

                            // Restar el descuento al total final
                            buyFromDb.Total = Math.Max(0, buyFromDb.Total - discount);

                            await _context.SaveChangesAsync();
                        }
                    }

                    // Limpiar el cupón de la sesión
                    HttpContext.Session.Remove("CurrentCouponCode");
                }

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

        // DTO para peticiones AJAX
        public class AddToCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } = 1;
            public string? ReturnUrl { get; set; }
        }

        private class AnonCartItem
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        // Endpoint AJAX que acepta JSON y soporta usuarios anónimos guardando en Session
        [HttpPost]
        [Route("ShoppingCarts/AddAjax")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> AddAjax([FromBody] AddToCartRequest req)
        {
            if (req == null || req.ProductId <= 0) return BadRequest(new { success = false, message = "Parámetros inválidos" });

            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
            {
                // Usuario anónimo -> almacenar en sesión
                try
                {
                    var json = HttpContext.Session.GetString("AnonCart") ?? "[]";
                    var list = JsonSerializer.Deserialize<List<AnonCartItem>>(json) ?? new List<AnonCartItem>();

                    var item = list.FirstOrDefault(i => i.ProductId == req.ProductId);
                    if (item == null)
                        list.Add(new AnonCartItem { ProductId = req.ProductId, Quantity = Math.Max(1, req.Quantity) });
                    else
                        item.Quantity += Math.Max(1, req.Quantity);

                    HttpContext.Session.SetString("AnonCart", JsonSerializer.Serialize(list));

                    return Json(new { success = true, anonymous = true, items = list.Count });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AddAjax (anon) falló");
                    return Json(new { success = false, message = "Error guardando carrito anónimo" });
                }
            }
            else
            {
                try
                {
                    await _cartService.AddToCartAsync(email, req.ProductId, Math.Max(1, req.Quantity));
                    return Json(new { success = true, anonymous = false });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al agregar al carrito para {Email}", email);
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
                return Challenge();

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["Error"] = "Debe ingresar un código de cupón.";
                return RedirectToAction("My");
            }

            code = code.Trim().ToUpper();

            // Obtener carrito del usuario
            var cart = await _cartService.GetCartByEmailAsync(email);
            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                TempData["Error"] = "No tiene productos en el carrito.";
                return RedirectToAction("My");
            }

            // Buscar cupón
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code && c.IsActive);

            if (coupon == null)
            {
                TempData["Error"] = "El cupón no existe o está inactivo.";
                return RedirectToAction("My");
            }

            var today = DateTime.Today;
            if (today < coupon.ValidFrom.Date || today > coupon.ValidTo.Date)
            {
                TempData["Error"] = "El cupón no está vigente.";
                return RedirectToAction("My");
            }

            
            HttpContext.Session.SetString("CurrentCouponCode", coupon.Code);

            TempData["Success"] = $"Cupón {coupon.Code} aplicado correctamente.";
            return RedirectToAction("My");
        }

    }
}