using System.Text.Json;
using AWSexam.Data;
using AWSexam.Models;
using Microsoft.EntityFrameworkCore;

namespace AWSexam.Services
{
    public class CartService
    {
        private const string SessionKey = "cart";
        private readonly StoreDbContext _db;

        public CartService(StoreDbContext db)
        {
            _db = db;
        }

        public Dictionary<int, int> Get(ISession session)
        {
            var json = session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json)) return new Dictionary<int, int>();
            return JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
        }

        public void Save(ISession session, Dictionary<int, int> cart)
        {
            session.SetString(SessionKey, JsonSerializer.Serialize(cart));
        }

        public void Clear(ISession session) => session.Remove(SessionKey);
        public async Task<CartVm> BuildAsync(ISession session)
        {
            var cart = Get(session);
            var ids = cart.Keys.ToList();

            var products = await _db.Products
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .OrderBy(p => p.Name)
                .ToListAsync();

            var vm = new CartVm();
            foreach (var p in products)
                vm.Lines.Add(new CartLineVm(p, cart[p.Id]));
            return vm;
        }
    }
}