using NUnit.Framework;
using ProductScraper;
using SellCatcher.Api.Services;

namespace SellCatcher.Tests.Services.ParserProductServiceTests
{
    [TestFixture]
    public class ParserProductServiceTests
    {
        private ParserProductService.ProductService _service;
        private Product _testProduct1;
        private Product _testProduct2;
        private Product _productForUpdate;

        private string _realDbPath;      // actual DB used by the service

        private const string TestStoreName = "TestStore";
        private const string TestCategory = "TestCat";

        // Compute real DB path *exactly the same way as the service*
        private string GetRealDbPath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var DBPlace = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));
            return Path.Combine(DBPlace, "sellcatcher.db");
        }

        [SetUp]
        public void Setup()
        {
            _realDbPath = GetRealDbPath();

            // Ensure service DB is clean BEFORE each test
            if (File.Exists(_realDbPath))
                File.Delete(_realDbPath);

            _service = new ParserProductService.ProductService();

            _testProduct1 = new Product
            {
                Name = "Product1",
                Price = 100,
                Category = TestCategory
            };

            _testProduct2 = new Product
            {
                Name = "Product2",
                Price = 200,
                Category = TestCategory
            };

            _productForUpdate = new Product
            {
                Name = "UpdateTest",
                Price = 100,
                Category = "Cat"
            };
        }

        [TearDown]
        public void TearDown()
        {
            // Ensure DB is removed AFTER each test
            if (File.Exists(_realDbPath))
                File.Delete(_realDbPath);
        }

        [Test]
        public void SaveProducts_NewProducts_InsertsCorrectly()
        {
            // Arrange
            var products = new List<Product> { _testProduct1, _testProduct2 };

            // Act
            var count = _service.SaveProducts(products, TestStoreName);

            // Assert
            Assert.That(count, Is.EqualTo(2));
            var store = _service.GetStoreByName(TestStoreName);
            Assert.That(store, Is.Not.Null);
            Assert.That(store.Categories.First().Products.Count, Is.EqualTo(2));
        }

        [Test]
        public void SaveProducts_ExistingProduct_UpdatesPrice()
        {
            // Arrange
            _service.SaveProducts(new List<Product> { _productForUpdate }, TestStoreName);

            var updatedProduct = new Product
            {
                Name = _productForUpdate.Name,
                Price = 150, // Changed price
                Category = _productForUpdate.Category
            };

            // Act
            _service.SaveProducts(new List<Product> { updatedProduct }, TestStoreName);

            // Assert
            var products = _service.GetProducts(TestStoreName, _productForUpdate.Category);
            Assert.That(products.Count, Is.EqualTo(1));
            Assert.That(products.First().Price, Is.EqualTo(150));
        }

        [Test]
        public void SaveProducts_ExistingProduct_UpdatesAllFields()
        {
            // Arrange
            var initialProduct = new Product
            {
                Name = "FullUpdate",
                Price = 100,
                OldPrice = 120,
                Discount = "-10%",
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(5),
                IsBulk = false,
                Category = "Cat"
            };
            _service.SaveProducts(new List<Product> { initialProduct }, "TestStore");

            var updatedProduct = new Product
            {
                Name = "FullUpdate",
                Price = 80,
                OldPrice = 100,
                Discount = "-20%",
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(10),
                IsBulk = true,
                BulkPrice = 75,
                Category = "Cat"
            };

            // Act
            _service.SaveProducts(new List<Product> { updatedProduct }, "TestStore");

            // Assert
            var products = _service.GetProducts("TestStore", "Cat");
            var saved = products.First();
            Assert.That(saved.Price, Is.EqualTo(80));
            Assert.That(saved.OldPrice, Is.EqualTo(100));
            Assert.That(saved.Discount, Is.EqualTo("-20%"));
            Assert.That(saved.IsBulk, Is.True);
            Assert.That(saved.BulkPrice, Is.EqualTo(75));
        }

        [Test]
        public void SaveProducts_NoCategorySpecified_UsesNonCategory()
        {
            // Arrange
            var product = new Product
            {
                Name = "NoCategory",
                Price = 100
                // Category is null
            };

            // Act
            _service.SaveProducts(new List<Product> { product }, "TestStore");

            // Assert
            var categories = _service.GetCategories("TestStore");
            Assert.That(categories.Any(c => c.Name == "non-category"), Is.True);

            var products = _service.GetProducts("TestStore", "non-category");
            Assert.That(products.Count, Is.EqualTo(1));
            Assert.That(products.First().Name, Is.EqualTo("NoCategory"));
        }

        [Test]
        public void SaveProducts_MultipleCategories_CreatesAll()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Name = "P1", Price = 10, Category = "Cat1" },
                new Product { Name = "P2", Price = 20, Category = "Cat2" },
                new Product { Name = "P3", Price = 30, Category = "Cat3" }
            };

            // Act
            _service.SaveProducts(products, "TestStore");

            // Assert
            var categories = _service.GetCategories("TestStore");
            Assert.That(categories.Count, Is.EqualTo(3));
            Assert.That(categories.Select(c => c.Name), Contains.Item("Cat1"));
            Assert.That(categories.Select(c => c.Name), Contains.Item("Cat2"));
            Assert.That(categories.Select(c => c.Name), Contains.Item("Cat3"));
        }

        [Test]
        public void SaveProducts_UpdatesWhenUpdatedTimestamp()
        {
            // Arrange
            var product = new Product
            {
                Name = "TimeTest",
                Price = 100,
                Category = "Cat"
            };
            _service.SaveProducts(new List<Product> { product }, "TestStore");

            var firstSave = _service.GetProducts("TestStore", "Cat").First();
            var firstTimestamp = firstSave.WhenUpdated;

            System.Threading.Thread.Sleep(1000); // Wait 1 second

            var updatedProduct = new Product
            {
                Name = "TimeTest",
                Price = 150,
                Category = "Cat"
            };

            // Act
            _service.SaveProducts(new List<Product> { updatedProduct }, "TestStore");

            // Assert
            var afterUpdate = _service.GetProducts("TestStore", "Cat").First();
            Assert.That(afterUpdate.WhenUpdated, Is.GreaterThan(firstTimestamp));
        }

        [Test]
        public void SaveProducts_NewStore_CreatesStoreAutomatically()
        {
            // Arrange
            var product = new Product
            {
                Name = "NewStoreProduct",
                Price = 50,
                Category = "Cat"
            };

            // Act
            _service.SaveProducts(new List<Product> { product }, "BrandNewStore");

            // Assert
            var store = _service.GetStoreByName("BrandNewStore");
            Assert.That(store, Is.Not.Null);
            Assert.That(store.Name, Is.EqualTo("BrandNewStore"));
        }

        [Test]
        public void GetStores_ReturnsAllStores()
        {
            // Arrange
            _service.SaveProducts(new List<Product>
            {
                new Product { Name = "P1", Price = 10, Category = "C" }
            }, "Store1");
            _service.SaveProducts(new List<Product>
            {
                new Product { Name = "P2", Price = 20, Category = "C" }
            }, "Store2");
            _service.SaveProducts(new List<Product>
            {
                new Product { Name = "P3", Price = 30, Category = "C" }
            }, "Store3");

            // Act
            var stores = _service.GetStores();

            // Assert
            Assert.That(stores.Count, Is.EqualTo(3));
        }

        [Test]
        public void GetStores_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var stores = _service.GetStores();

            // Assert
            Assert.That(stores, Is.Empty);
        }

        [Test]
        public void GetStoreByName_ExistingStore_ReturnsStore()
        {
            // Arrange
            _service.SaveProducts(new List<Product>
            {
                new Product { Name = "P", Price = 10, Category = "C" }
            }, "FindMe");

            // Act
            var store = _service.GetStoreByName("FindMe");

            // Assert
            Assert.That(store, Is.Not.Null);
            Assert.That(store.Name, Is.EqualTo("FindMe"));
        }

        [Test]
        public void GetStoreByName_NonExistentStore_ReturnsNull()
        {
            // Act
            var store = _service.GetStoreByName("DoesNotExist");

            // Assert
            Assert.That(store, Is.Null);
        }

        [Test]
        public void GetStoreByName_CaseInsensitive_FindsStore()
        {
            // Arrange
            _service.SaveProducts(new List<Product>
            {
                new Product { Name = "P", Price = 10, Category = "C" }
            }, "TestStore");

            // Act
            var store = _service.GetStoreByName("teststore");

            // Assert
            Assert.That(store, Is.Not.Null);
        }

        [Test]
        public void GetCategories_ValidStore_ReturnsAllCategories()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Name = "P1", Price = 10, Category = "Electronics" },
                new Product { Name = "P2", Price = 20, Category = "Food" },
                new Product { Name = "P3", Price = 30, Category = "Clothing" }
            };
            _service.SaveProducts(products, "TestStore");

            // Act
            var categories = _service.GetCategories("TestStore");

            // Assert
            Assert.That(categories.Count, Is.EqualTo(3));
        }

        [Test]
        public void GetCategories_NonExistentStore_ReturnsEmptyList()
        {
            // Act
            var categories = _service.GetCategories("NonExistent");

            // Assert
            Assert.That(categories, Is.Empty);
        }

        [Test]
        public void GetProducts_ValidStoreAndCategory_ReturnsProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Name = "Laptop", Price = 1000, Category = "Electronics" },
                new Product { Name = "Phone", Price = 500, Category = "Electronics" }
            };
            _service.SaveProducts(products, "TestStore");

            // Act
            var result = _service.GetProducts("TestStore", "Electronics");

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetProducts_NonExistentCategory_ReturnsEmptyList()
        {
            // Arrange
            _service.SaveProducts(new List<Product>
            {
                new Product { Name = "P", Price = 10, Category = "Cat1" }
            }, "TestStore");

            // Act
            var result = _service.GetProducts("TestStore", "NonExistentCategory");

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetOnSale_AllCategories_ReturnsOnlySaleItems()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product
                {
                    Name = "OnSale1",
                    Price = 50,
                    IsOnSale = true,
                    ValidUntil = DateTime.UtcNow.AddDays(5),
                    Category = "Cat1"
                },
                new Product
                {
                    Name = "NotOnSale",
                    Price = 100,
                    IsOnSale = false,
                    Category = "Cat1"
                },
                new Product
                {
                    Name = "OnSale2",
                    Price = 75,
                    IsOnSale = true,
                    ValidUntil = DateTime.UtcNow.AddDays(3),
                    Category = "Cat2"
                }
            };
            _service.SaveProducts(products, "TestStore");

            // Act
            var result = _service.GetOnSale("TestStore");

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(p => p.IsOnSale), Is.True);
        }

        [Test]
        public void GetOnSale_SpecificCategory_ReturnsOnlyCategorySales()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product
                {
                    Name = "Cat1Sale",
                    Price = 50,
                    IsOnSale = true,
                    ValidUntil = DateTime.UtcNow.AddDays(5),
                    Category = "Cat1"
                },
                new Product
                {
                    Name = "Cat2Sale",
                    Price = 100,
                    IsOnSale = true,
                    ValidUntil = DateTime.UtcNow.AddDays(5),
                    Category = "Cat2"
                }
            };
            _service.SaveProducts(products, "TestStore");

            // Act
            var result = _service.GetOnSale("TestStore", "Cat1");

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().Name, Is.EqualTo("Cat1Sale"));
        }

        [Test]
        public void GetOnSale_ExcludesExpiredSales()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product
                {
                    Name = "ActiveSale",
                    Price = 50,
                    IsOnSale = true,
                    ValidUntil = DateTime.UtcNow.AddDays(5),
                    Category = "Cat"
                },
                new Product
                {
                    Name = "ExpiredSale",
                    Price = 100,
                    IsOnSale = true,
                    ValidUntil = DateTime.UtcNow.AddDays(-1),
                    Category = "Cat"
                }
            };
            _service.SaveProducts(products, "TestStore");

            // Act
            var result = _service.GetOnSale("TestStore");

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().Name, Is.EqualTo("ActiveSale"));
        }

        [Test]
        public void GetOnSale_IncludesSalesWithoutExpiry()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product
                {
                    Name = "NoExpirySale",
                    Price = 50,
                    IsOnSale = true,
                    ValidUntil = null, // No expiry
                    Category = "Cat"
                }
            };
            _service.SaveProducts(products, "TestStore");

            // Act
            var result = _service.GetOnSale("TestStore");

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().Name, Is.EqualTo("NoExpirySale"));
        }

        [Test]
        public void GetOnSale_NonExistentStore_ReturnsEmptyList()
        {
            // Act
            var result = _service.GetOnSale("NonExistent");

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SaveProducts_LargeNumberOfProducts_HandlesCorrectly()
        {
            // Arrange
            var products = Enumerable.Range(1, 100).Select(i => new Product
            {
                Name = $"Product{i}",
                Price = i * 10,
                Category = $"Cat{i % 5}" // 5 different categories
            }).ToList();

            // Act
            var count = _service.SaveProducts(products, "TestStore");

            // Assert
            Assert.That(count, Is.EqualTo(100));
            var categories = _service.GetCategories("TestStore");
            Assert.That(categories.Count, Is.EqualTo(5));
        }
    }
}