using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProyectoEcommerce.Services;

namespace ProyectoEcommerce.ViewComponents
{
    public class CartTodoViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;
        public CartTodoViewComponent(ICartService cartService) => _cartService = cartService;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var email = HttpContext?.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return View(null);

            var cart = await _cartService.GetCartByEmailAsync(email);
            return View(cart);
        }
    }
}