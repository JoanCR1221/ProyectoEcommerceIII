using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoEcommerce.Controllers
{
    public class FavoritesController : Controller
    {
        private readonly ProyectoEcommerceContext _context;

        public FavoritesController(ProyectoEcommerceContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // 1. Obtener IDs de favoritos de la sesión
                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favoriteIds = JsonSerializer.Deserialize<List<int>>(favoritesJson);

                // 2. Si no hay favoritos, mostrar vista vacía
                if (favoriteIds == null || !favoriteIds.Any())
                {
                    ViewBag.FavoritesCount = 0;
                    return View(new List<Product>());
                }

                // 3. CARGAR PRODUCTOS COMPLETOS desde la base de datos
                var favoriteProducts = await _context.Products
                    .Where(p => favoriteIds.Contains(p.Id))
                    .ToListAsync();

                ViewBag.FavoritesCount = favoriteProducts.Count;

                // 4. Pasar los productos COMPLETOS a la vista
                return View(favoriteProducts);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar los favoritos: " + ex.Message;
                return View(new List<Product>());
            }
        }



        [HttpPost]
        public IActionResult AddToFavorites([FromBody] int productId)
        {
            try
            {
                // Obtener favoritos actuales de la sesión
                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favorites = JsonSerializer.Deserialize<List<int>>(favoritesJson);

                // Verificar si ya está en favoritos
                if (!favorites.Contains(productId))
                {
                    favorites.Add(productId);
                    // Guardar en sesión
                    HttpContext.Session.SetString("Favorites", JsonSerializer.Serialize(favorites));

                    return Json(new
                    {
                        success = true,
                        message = "Producto agregado a favoritos",
                        isFavorite = true
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "El producto ya está en favoritos",
                        isFavorite = true
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }

        [HttpPost]
        public IActionResult RemoveFromFavorites([FromBody] int productId)
        {
            try
            {
                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favorites = JsonSerializer.Deserialize<List<int>>(favoritesJson);

                favorites.Remove(productId);
                HttpContext.Session.SetString("Favorites", JsonSerializer.Serialize(favorites));

                return Json(new
                {
                    success = true,
                    message = "Producto removido de favoritos",
                    isFavorite = false
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }

        [HttpPost]
        public IActionResult ToggleFavorite([FromBody] int productId)
        {
            try
            {
                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favorites = JsonSerializer.Deserialize<List<int>>(favoritesJson);

                if (favorites.Contains(productId))
                {
                    // Remover si ya existe
                    favorites.Remove(productId);
                    HttpContext.Session.SetString("Favorites", JsonSerializer.Serialize(favorites));
                    return Json(new
                    {
                        success = true,
                        isFavorite = false,
                        message = "Removido de favoritos"
                    });
                }
                else
                {
                    // Agregar si no existe
                    favorites.Add(productId);
                    HttpContext.Session.SetString("Favorites", JsonSerializer.Serialize(favorites));
                    return Json(new
                    {
                        success = true,
                        isFavorite = true,
                        message = "Agregado a favoritos"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetFavorites()
        {
            try
            {
                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favorites = JsonSerializer.Deserialize<List<int>>(favoritesJson);

                return Json(new
                {
                    success = true,
                    favorites = favorites
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetFavoritesCount()
        {
            try
            {
                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favorites = JsonSerializer.Deserialize<List<int>>(favoritesJson);

                return Json(new
                {
                    success = true,
                    count = favorites.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message,
                    count = 0
                });
            }
        }

        [HttpGet]
        public IActionResult CheckFavorite(int productId)
        {
            try
            {
                var favoritesJson = HttpContext.Session.GetString("Favorites") ?? "[]";
                var favorites = JsonSerializer.Deserialize<List<int>>(favoritesJson);
                var isFavorite = favorites.Contains(productId);

                return Json(new
                {
                    success = true,
                    isFavorite = isFavorite
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }
    }
}