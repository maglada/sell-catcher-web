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
