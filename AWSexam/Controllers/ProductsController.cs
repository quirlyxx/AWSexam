using AWSexam.Models;
using AWSexam.Services;
using AWSexam.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AWSexam.Controllers
{
    public class ProductsController : Controller
    {
        private readonly StoreDbContext _db;
        private readonly ImageStorageService _images;

        public ProductsController(StoreDbContext db, ImageStorageService images)
        {
            _db = db;
            _images = images;
        }

        public async Task<IActionResult> Index(string? category)
        {
            var query = _db.Products.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category == category);

            var products = await query.OrderBy(p => p.Name).ToListAsync();
            var categories = await _db.Products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var vm = new ProductListVm
            {
                Items = products.Select(p => new ProductCardViewModel(p, _images.GetUrl(p.ImageKey))).ToList(),
                Categories = categories,
                SelectedCategory = category
            };
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if(product==null)
            {
                return NotFound();
            }
            return View(new ProductCardViewModel(product, _images.GetUrl(product.ImageKey)));
        }
        public IActionResult Create() => View(new Product());
        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Category,Description,Price,Quantity")] Product product, IFormFile image)
        {
            if(product.Price < 0)
            {
                ModelState.AddModelError(nameof(product.Price), "Ціна не може бути від'ємною");

            }
            if (product.Quantity < 0)
            {
                ModelState.AddModelError(nameof(product.Quantity), "Кількість не може бути від'ємною");
            }
            var hasImage = image != null && image.Length > 0;
            if(hasImage)
            {
                var error = _images.Validate(image);
                if (error != null)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }
            if(!ModelState.IsValid)
            {
                return View(product);
            }
           if(hasImage)
           {
                product.ImageKey = await _images.Upload(image!);


            }
           _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = product.Id });
        }
    }
}
