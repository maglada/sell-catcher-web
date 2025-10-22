using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public class DiscountService
    {
        private readonly string _dbPath;

        private readonly List<Discount> _discounts = new()
        {
            new Discount { Id = 1, StoreId = 1, Product = "Stella Artois 0.5л", OldPrice = 35, NewPrice = 29.5m, ValidUntil = DateTime.UtcNow.AddDays(7) },
            new Discount { Id = 2, StoreId = 2, Product = "Хліб білий", OldPrice = 20, NewPrice = 15, ValidUntil = DateTime.UtcNow.AddDays(5) },
            new Discount { Id = 3, StoreId = 3, Product = "Козацька рада 0.5л", OldPrice = 80, NewPrice = 65, ValidUntil = DateTime.UtcNow.AddDays(10) }
        };

        public IEnumerable<Discount> GetAll() => _discounts;

        public IEnumerable<Discount> GetByStore(int storeId) =>
            _discounts.Where(d => d.StoreId == storeId);

        public Discount? GetById(int id) => _discounts.FirstOrDefault(d => d.Id == id);

        public void Add(Discount discount)
        {
            discount.Id = _discounts.Max(d => d.Id) + 1;
            _discounts.Add(discount);


        public DiscountService()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var DBPlace = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));

            _dbPath = Path.Combine(DBPlace, "sellcatcher.db");
        }

        public List<NOVUSProduct> GetAll()
        {
            using var db = new LiteDatabase(_dbPath);
            var stores = db.GetCollection<Store>("stores").FindAll();

            var allProducts = stores.SelectMany(s => s.Categories).SelectMany(c => c.Products).Where(p => p.IsOnSale && (p.ValidUntil == null || p.ValidUntil > DateTime.Now)).ToList();

            return allProducts;
        }

        public List<NOVUSProduct> GetByStore(int storeId)
        {
            using var db = new LiteDatabase(_dbPath);
            var store = db.GetCollection<Store>("stores").FindById(storeId);
            if (store == null) return new List<NOVUSProduct>();

            return store.Categories.SelectMany(c => c.Products).Where(p => p.IsOnSale && (p.ValidUntil == null || p.ValidUntil > DateTime.Now)).ToList();
        }

        public NOVUSProduct? GetById(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            var stores = db.GetCollection<Store>("stores");

            foreach (var store in stores.FindAll())
            {
                foreach (var category in store.Categories)
                {
                    var product = category.Products.FirstOrDefault(p => p.Id == id);

                    if (product != null && (product.IsOnSale || !string.IsNullOrEmpty(product.Discount)))
                        return product;
                }
            }
            return null;
        }

        public void Add(NOVUSProduct discount)
        {
            using var db = new LiteDatabase(_dbPath);
            var stores = db.GetCollection<Store>("stores");

            var store = stores.FindOne(x => x.Name == "NOVUS") ?? new Store { Name = "NOVUS" };
            var category = store.Categories.FirstOrDefault() ?? new Category { Name = "General" };

            int nextId = store.Categories.SelectMany(c => c.Products).Select(p => p.Id).DefaultIfEmpty(0).Max() + 1;

            discount.Id = nextId;
            category.Products.Add(discount);

            if (!store.Categories.Any())
                store.Categories.Add(category);

            stores.Update(store);
        }
    }
}
