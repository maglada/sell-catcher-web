using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using SellCatcher.Api.Models;
using ProductScraper;

public class DiscountService
{
    private readonly string _dbPath;

    public DiscountService()
    {
        // Check multiple possible database locations
        var possiblePaths = new[]
        {
            "/app/data/sellcatcher.db",                                    // Docker volume path
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "sellcatcher.db"),
            Path.Combine(Directory.GetCurrentDirectory(), "sellcatcher.db"),
            "sellcatcher.db"
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Test if we can access it
                using var testDb = new LiteDatabase(path);
                _dbPath = path;
                Console.WriteLine($"DiscountService using database at: {_dbPath}");
                return;
            }
            catch
            {
                continue;
            }
        }

        // Fallback
        _dbPath = "/app/data/sellcatcher.db";
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }


        public List<Product> GetAll()
        {
            using var db = new LiteDatabase(_dbPath);
            var stores = db.GetCollection<Store>("stores").FindAll();

            var allProducts = stores.SelectMany(s => s.Categories).SelectMany(c => c.Products).Where(p => p.IsOnSale && (p.ValidUntil == null || p.ValidUntil > DateTime.Now)).ToList();

            return allProducts;
        }

        public List<Product> GetByStore(int storeId)
        {
            using var db = new LiteDatabase(_dbPath);
            var store = db.GetCollection<Store>("stores").FindById(storeId);
            if (store == null) return new List<Product>();

            return store.Categories.SelectMany(c => c.Products).Where(p => p.IsOnSale && (p.ValidUntil == null || p.ValidUntil > DateTime.Now)).ToList();
        }

        public Product? GetById(Guid id)
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

        public void Add(string storeName, Product discount)
        {
            using var db = new LiteDatabase(_dbPath);
            var stores = db.GetCollection<Store>("stores");
            var store = stores.FindOne(x => x.Name == storeName);
            if (store == null)
            {
                store = new Store
                {
                    Name = storeName,
                    Categories = new List<Category>()
                };
            }

            var category = store.Categories.FirstOrDefault(c => c.Name == discount.Category);
            if (category == null)
            {
                category = new Category
                {
                    Name = discount.Category,
                    Products = new List<Product>()
                };
                store.Categories.Add(category);
            }
            category.Products ??= new List<Product>();
            discount.Id = Guid.NewGuid(); 
            category.Products.Add(discount);
            stores.Upsert(store);
        }
        public List<Product> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Product>();

            query = query.ToLower().Trim();

            using var db = new LiteDatabase(_dbPath);
            var stores = db.GetCollection<Store>("stores").FindAll();

            var products = stores
                .SelectMany(s => s.Categories)
                .SelectMany(c => c.Products)
                .Where(p =>                    (
                        (!string.IsNullOrEmpty(p.Name) && p.Name.ToLower().Contains(query)) ||
                        (!string.IsNullOrEmpty(p.Category) && p.Category.ToLower().Contains(query))
                    )
                )
                .ToList();

            return products;
        }

    }

