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
    /// Service responsible for saving, updating and retrieving parsed product data from multiple stores
    public class ParserProductService
    {
        public class ProductService
        {
            /// Where is the database
            private readonly string _dbPath;
            public ProductService()
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                var DBPlace = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));

                _dbPath = Path.Combine(DBPlace, "sellcatcher.db");
            }

            /// <summary>
            /// Saves or updates the list of goods in the LiteDB database for the specified store
            /// </summary>
            /// <param name="parserProducts">List of parsing goods</param>
            /// <param name="storeName">Store name, which get products</param>
            /// <returns>Amount of goods</returns>
            public int SaveProducts(List<Product> parserProducts, string storeName = "STORE")
            {
                using var db = new LiteDatabase(_dbPath);
                var stores = db.GetCollection<Store>("stores");

                /// Find existing store or create new one
                var store = stores.FindOne(x => x.Name.Equals(storeName, StringComparison.OrdinalIgnoreCase));
                if (store == null)
                {
                    store = new Store { Name = storeName };
                    stores.Insert(store);
                }

                /// Go through every product. If the category is not specified then non-category
                foreach (var product in parserProducts)
                {
                    var categoryName = product.Category ?? "non-category";

                    /// Check if there is such a category in the store.
                    var category = store.Categories.FirstOrDefault(c => c.Name == categoryName);
                    if (category == null)
                    {
                        category = new Category { Name = categoryName };
                        store.Categories.Add(category);
                    }

                    /// If the product is already in the category (by name), update all fields
                    var alreadyExist = category.Products.FirstOrDefault(p => p.Name == product.Name);
                    if (alreadyExist != null)
                    {
                        alreadyExist.Price = product.Price;
                        alreadyExist.OldPrice = product.OldPrice;
                        alreadyExist.Discount = product.Discount;
                        alreadyExist.IsOnSale = product.IsOnSale;
                        alreadyExist.ValidUntil = product.ValidUntil;
                        alreadyExist.IsBulk = product.IsBulk;
                        alreadyExist.BulkPrice = product.BulkPrice;
                        alreadyExist.ImageUrl = product.ImageUrl;
                        alreadyExist.SourceImg = product.SourceImg;
                        alreadyExist.WhenUpdated = DateTime.Now;
                    }
                    else
                    {

                        /// Good creation
                        category.Products.Add(new Product
                        {
                            Name = product.Name,
                            Price = product.Price,
                            OldPrice = product.OldPrice,
                            Discount = product.Discount,
                            IsOnSale = product.IsOnSale,
                            ValidUntil = product.ValidUntil,
                            Category = categoryName,
                            IsBulk = product.IsBulk,
                            BulkPrice = product.BulkPrice,
                            ImageUrl = product.ImageUrl,
                            SourceImg = product.SourceImg,
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
            public List<Product> GetProducts(string storeName, string categoryName)
            {
                using var db = new LiteDatabase(_dbPath);
                var store = db.GetCollection<Store>("stores").FindOne(x => x.Name.Equals(storeName, StringComparison.OrdinalIgnoreCase));
                var category = store?.Categories.FirstOrDefault(c => c.Name == categoryName);
                return category?.Products ?? new List<Product>();
            }

            // Get all discounts
            public List<Product> GetOnSale(string storeName, string categoryName = null)
            {
                using var db = new LiteDatabase(_dbPath);
                var store = db.GetCollection<Store>("stores").FindOne(x => x.Name.Equals(storeName, StringComparison.OrdinalIgnoreCase));

                if (store == null)
                    return new List<Product>();

                var allProducts = string.IsNullOrEmpty(categoryName) ? store.Categories.SelectMany(c => c.Products)
                                  : store.Categories.FirstOrDefault(c => c.Name == categoryName)?.Products ?? new List<Product>();

                return allProducts.Where(p => p.IsOnSale && (p.ValidUntil == null || p.ValidUntil > DateTime.Now)).ToList();
            }
        }
    }
}


