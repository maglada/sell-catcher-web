using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using SellCatcher.Api.Models;
using ProductScraper;

namespace SellCatcher.Api.Services
{
    public class ParserProductService
    {
        public class ProductService
        {
            private readonly string _dbPath;
            public ProductService()
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                var DBPlace = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));

                _dbPath = Path.Combine(DBPlace, "sellcatcher.db");
            }

            public int SaveProducts(List<Product> parserProducts, string storeName = "NOVUS")
            {
                using var db = new LiteDatabase(_dbPath);
                var stores = db.GetCollection<Store>("stores");

                var store = stores.FindOne(x => x.Name == storeName);
                if (store == null)
                {
                    store = new Store { Name = storeName };
                    stores.Insert(store);
                }

                foreach (var product in parserProducts)
                {
                    var categoryName = product.Category ?? "non-category";
                    var category = store.Categories.FirstOrDefault(c => c.Name == categoryName);
                    if (category == null)
                    {
                        category = new Category { Name = categoryName };
                        store.Categories.Add(category);
                    }

                    var alreadyExist = category.Products.FirstOrDefault(p => p.Name == product.Name);
                    if (alreadyExist != null)
                    {
                        alreadyExist.Price = product.Price;
                        alreadyExist.OldPrice = product.OldPrice;
                        alreadyExist.Discount = product.Discount;
                        alreadyExist.IsOnSale = product.IsOnSale;
                        alreadyExist.ValidUntil = ParseDate(product.ValidUntil);
                        alreadyExist.WhenUpdated = DateTime.Now;
                    }
                    else
                    {
                        category.Products.Add(new NOVUSProduct
                        {
                            Name = product.Name,
                            Price = product.Price,
                            OldPrice = product.OldPrice,
                            Discount = product.Discount,
                            IsOnSale = product.IsOnSale,
                            ValidUntil = ParseDate(product.ValidUntil),
                            Category = categoryName,
                            WhenUpdated = DateTime.Now
                        });
                    }
                }

                stores.Update(store);
                return parserProducts.Count;
            }


            // Get all stores
            public List<Store> GetStores()
            {
                using var db = new LiteDatabase(_dbPath);
                return db.GetCollection<Store>("stores").FindAll().ToList();
            }

            // Get specific story by name
            public Store? GetStoreByName(string storeName)
            {
                using var db = new LiteDatabase(_dbPath);
                return db.GetCollection<Store>("stores").FindOne(x => x.Name.Equals(storeName, StringComparison.OrdinalIgnoreCase));
            }

            // Get all categories in specific store
            public List<Category> GetCategories(string storeName)
            {
                using var db = new LiteDatabase(_dbPath);
                var store = db.GetCollection<Store>("stores").FindOne(x => x.Name.Equals(storeName, StringComparison.OrdinalIgnoreCase));
                return store?.Categories ?? new List<Category>();
            }

            // Get all products in specific category
            public List<NOVUSProduct> GetProducts(string storeName, string categoryName)
            {
                using var db = new LiteDatabase(_dbPath);
                var store = db.GetCollection<Store>("stores").FindOne(x => x.Name.Equals(storeName, StringComparison.OrdinalIgnoreCase));
                var category = store?.Categories.FirstOrDefault(c => c.Name == categoryName);
                return category?.Products ?? new List<NOVUSProduct>();
            }

            // Get all discounts
            public List<NOVUSProduct> GetOnSale(string storeName, string categoryName = null)
            {
                using var db = new LiteDatabase(_dbPath);
                var store = db.GetCollection<Store>("stores").FindOne(x => x.Name.Equals(storeName, StringComparison.OrdinalIgnoreCase));

                if (store == null)
                    return new List<NOVUSProduct>();

                var allProducts = string.IsNullOrEmpty(categoryName) ? store.Categories.SelectMany(c => c.Products)
                                  : store.Categories.FirstOrDefault(c => c.Name == categoryName)?.Products ?? new List<NOVUSProduct>();

                return allProducts.Where(p => p.IsOnSale && (p.ValidUntil == null || p.ValidUntil > DateTime.Now)).ToList();
            }

            private DateTime? ParseDate(string dateStr)
            {
                if (string.IsNullOrEmpty(dateStr))
                    return null;

                try
                {
                    var parts = dateStr.Split('.');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int day) &&
                        int.TryParse(parts[1], out int month))
                    {
                        var year = DateTime.Now.Year;
                        var date = new DateTime(year, month, day);
                        if (date < DateTime.Now)
                            date = date.AddYears(1);
                        return date;
                    }
                }
                catch { }

                return null;
            }
        }
    }
}


