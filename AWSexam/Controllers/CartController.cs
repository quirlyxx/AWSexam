using AWSexam.Data;
using AWSexam.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AWSexam.Controllers
{
    public class CartController : Controller
    {
        private readonly StoreDbContext _db;
        private readonly CartService _cart;

        public CartController(StoreDbContext db, CartService cart)
        {
            _db = db;
            _cart = cart;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _cart.BuildAsync(HttpContext.Session));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id)
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            var cart = _cart.Get(HttpContext.Session);
            cart.TryGetValue(id, out var current);

            if (current + 1 > product.Quantity)
            {
                TempData["Error"] = $"Більше {product.Quantity} шт. товару «{product.Name}» додати не можна.";
            }
            else
            {
                cart[id] = current + 1;
                _cart.Save(HttpContext.Session, cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int quantity)
        {
            var cart = _cart.Get(HttpContext.Session);
            if (!cart.ContainsKey(id)) return RedirectToAction(nameof(Index));

            if (quantity <= 0)
            {
                cart.Remove(id);
            }
            else
            {
                var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                var max = product?.Quantity ?? 0;

                if (max == 0)
                {
                    cart.Remove(id);
                }
                else
                {
                    if (quantity > max)
                        TempData["Error"] = $"У наявності лише {max} шт. товару «{product!.Name}».";
                    cart[id] = Math.Min(quantity, max);
                }
            }

            _cart.Save(HttpContext.Session, cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = _cart.Get(HttpContext.Session);
            cart.Remove(id);
            _cart.Save(HttpContext.Session, cart);
            return RedirectToAction(nameof(Index));
        }
    }
}