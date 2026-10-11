using AWSexam.Data;
using AWSexam.Models;
using AWSexam.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AWSexam.Controllers
{
    public class OrdersController : Controller
    {
        private readonly StoreDbContext _db;
        private readonly CartService _cart;
        private readonly OrderQueueService _queue;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            StoreDbContext db,
            CartService cart,
            OrderQueueService queue,
            ILogger<OrdersController> logger)
        {
            _db = db;
            _cart = cart;
            _queue = queue;
            _logger = logger;
        }

        public async Task<IActionResult> Checkout()
        {
            var cart = await _cart.BuildAsync(HttpContext.Session);
            if (!cart.Lines.Any())
                return RedirectToAction("Index", "Cart");

            return View(new CheckoutVm { Cart = cart });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutVm model)
        {
            var cartItems = _cart.Get(HttpContext.Session);
            if (cartItems.Count == 0)
                return RedirectToAction("Index", "Cart");

            var ids = cartItems.Keys.ToList();
            var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

            foreach (var (id, qty) in cartItems)
            {
                var product = products.FirstOrDefault(p => p.Id == id);
                if (product == null)
                    ModelState.AddModelError(string.Empty, "Один із товарів у кошику більше не доступний.");
                else if (qty > product.Quantity)
                    ModelState.AddModelError(string.Empty,
                        $"Товару «{product.Name}» недостатньо на складі (є {product.Quantity} шт.).");
            }

            if (!ModelState.IsValid)
            {
                model.Cart = await _cart.BuildAsync(HttpContext.Session);
                return View(model);
            }

            var order = new Order
            {
                CustomerName = model.CustomerName,
                CustomerEmail = model.Email,
                CustomerPhone = model.Phone,
                CreatedAt = DateTime.UtcNow,
                Status = "New"
            };

            foreach (var (id, qty) in cartItems)
            {
                var product = products.First(p => p.Id == id);
                order.Items.Add(new OrderItem
                {
                    Product = product,
                    Quantity = qty,
                    UnitPrice = product.Price
                });
                product.Quantity -= qty;
            }

            order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            try
            {
                await _queue.SendOrderCreatedAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не вдалося відправити замовлення {OrderId} в SQS", order.Id);
            }

            _cart.Clear(HttpContext.Session);
            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}