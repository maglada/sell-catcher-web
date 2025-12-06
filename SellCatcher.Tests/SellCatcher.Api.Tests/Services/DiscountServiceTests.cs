using NUnit.Framework;
using ProductScraper;
using SellCatcher.Api.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SellCatcher.Tests.Services.DiscountServiceTests
{
    [TestFixture]
    public class DiscountServiceTests
    {
        private DiscountService _service;
        private Product _activeMilk;
        private Product _expiredBread;
        private Product _nonSaleTea;
        private Product _noExpiryCoffee;

        private const string TestDbFileName = "test_discount.db";
        private const string TestStoreName = "TestStore";

        private string _fullDbPath;      // unused test DB
        private string _realDbPath;      // actual DB used by DiscountService

        private string GetRealDbPath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var DBPlace = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));
            return Path.Combine(DBPlace, "sellcatcher.db");
        }

        [SetUp]
        public void Setup()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var DBPlace = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));
            _fullDbPath = Path.Combine(DBPlace, TestDbFileName);

            _realDbPath = GetRealDbPath();


            if (File.Exists(_realDbPath))
                File.Delete(_realDbPath);

            if (File.Exists(_fullDbPath))
                File.Delete(_fullDbPath);

            _service = new DiscountService();

            // Initialize products
            _activeMilk = new Product
            {
                Name = "Milk",
                Price = 20,
                OldPrice = 30,
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(5),
                Category = "Dairy"
            };

            _expiredBread = new Product
            {
                Name = "Bread",
                Price = 10,
                OldPrice = 15,
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(-1),
                Category = "Bakery"
            };

            _nonSaleTea = new Product
            {
                Name = "Tea",
                Price = 50,
                IsOnSale = false,
                Category = "Beverages"
            };

            _noExpiryCoffee = new Product
            {
                Name = "Coffee",
                Price = 100,
                OldPrice = 120,
                IsOnSale = true,
                ValidUntil = null,
                Category = "Beverages"
            };

            SeedTestData();
        }

        private void SeedTestData()
        {
            var products = new List<Product> { _activeMilk, _expiredBread, _nonSaleTea, _noExpiryCoffee };
            foreach (var product in products)
            {
                _service.Add(TestStoreName, product);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_realDbPath))
                File.Delete(_realDbPath);

            if (File.Exists(_fullDbPath))
                File.Delete(_fullDbPath);
        }


        [Test]
        public void GetAll_ReturnsOnlyActiveDiscounts()
        {
            var result = _service.GetAll();

            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Any(p => p.Name == "Milk"));
            Assert.That(result.Any(p => p.Name == "Coffee"));
        }

        [Test]
        public void GetAll_ExcludesExpiredDiscounts()
        {
            var result = _service.GetAll();
            Assert.That(result.Any(p => p.Name == "Bread"), Is.False);
        }

        [Test]
        public void GetAll_ExcludesNonSaleItems()
        {
            var result = _service.GetAll();
            Assert.That(result.Any(p => p.Name == "Tea"), Is.False);
        }

        [Test]
        public void GetAll_IncludesDiscountsWithoutExpiryDate()
        {
            var result = _service.GetAll();
            Assert.That(result.Any(p => p.Name == "Coffee"), Is.True);
        }

        [Test]
        public void GetById_ExistingProduct_ReturnsProduct()
        {
            var id = _service.GetAll().First().Id;
            var result = _service.GetById(id);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(id));
        }

        [Test]
        public void GetById_NonExistentId_ReturnsNull()
        {
            var result = _service.GetById(Guid.NewGuid());
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetById_ReturnsProductWithCorrectDetails()
        {
            var milk = _service.GetAll().First(p => p.Name == "Milk");
            var result = _service.GetById(milk.Id);

            Assert.That(result.Name, Is.EqualTo("Milk"));
            Assert.That(result.Price, Is.EqualTo(20));
            Assert.That(result.OldPrice, Is.EqualTo(30));
            Assert.That(result.Category, Is.EqualTo("Dairy"));
        }

        [Test]
        public void Add_ValidProduct_SavesSuccessfully()
        {
            var p = new Product
            {
                Name = "New Product",
                Price = 100,
                OldPrice = 150,
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(10),
                Category = "Electronics"
            };

            _service.Add(TestStoreName, p);

            Assert.That(_service.GetAll().Any(x => x.Name == "New Product"), Is.True);
        }

        [Test]
        public void Add_ProductToNewStore_CreatesStore()
        {
            var p = new Product
            {
                Name = "Product for New Store",
                Price = 50,
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(5),
                Category = "Test"
            };

            _service.Add("BrandNewStore", p);

            Assert.That(_service.GetAll().Any(x => x.Name == "Product for New Store"), Is.True);
        }

        [Test]
        public void Add_ProductWithNewCategory_CreatesCategory()
        {
            var p = new Product
            {
                Name = "New Category Product",
                Price = 75,
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(3),
                Category = "BrandNewCategory"
            };

            _service.Add(TestStoreName, p);

            Assert.That(_service.GetAll().Any(x => x.Category == "BrandNewCategory"), Is.True);
        }

        [Test]
        public void Add_ProductWithDiscount_SavesCorrectly()
        {
            var p = new Product
            {
                Name = "Discounted Item",
                Price = 50,
                Discount = "-20%",
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(7),
                Category = "Sale"
            };

            _service.Add(TestStoreName, p);

            var saved = _service.GetAll().First(x => x.Name == "Discounted Item");

            Assert.That(saved.Discount, Is.EqualTo("-20%"));
        }

        //[Test]
        //public void Add_ProductWithoutCategory_DefaultsToEmptyCategory()
        //{
        //    var p = new Product
        //    {
        //        Name = "No Category Product",
        //        Price = 30,
        //        IsOnSale = true,
        //        ValidUntil = DateTime.UtcNow.AddDays(2)
        //    };

        //    _service.Add(TestStoreName, p);

        //    var saved = _service.GetAll().First(x => x.Name == "No Category Product");

        //    Assert.That(saved.Category, Is.Not.Null);
        //}

        [Test]
        public void GetByStore_ValidStoreId_ReturnsOnlyStoreProducts()
        {
            var storeId = 1;

            _service.Add("Store1", new Product
            {
                Name = "Store1 Product",
                Price = 10,
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(5),
                Category = "Test"
            });

            _service.Add("Store2", new Product
            {
                Name = "Store2 Product",
                Price = 20,
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(5),
                Category = "Test"
            });

            var store1Products = _service.GetByStore(storeId);

            Assert.That(store1Products, Is.Not.Empty);
        }

        [Test]
        public void GetByStore_NonExistentStore_ReturnsEmptyList()
        {
            Assert.That(_service.GetByStore(9999), Is.Empty);
        }

        [Test]
        public void GetByStore_FiltersExpiredDiscounts()
        {
            var storeId = 1;
            var result = _service.GetByStore(storeId);

            Assert.That(result.Any(p => p.Name == "Bread"), Is.False);
        }

        [Test]
        public void Add_MultipleProductsSameCategory_GroupsCorrectly()
        {
            var p1 = new Product
            {
                Name = "Category Test 1",
                Price = 10,
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(5),
                Category = "SameCategory"
            };

            var p2 = new Product
            {
                Name = "Category Test 2",
                Price = 20,
                IsOnSale = true,
                ValidUntil = DateTime.UtcNow.AddDays(5),
                Category = "SameCategory"
            };

            _service.Add(TestStoreName, p1);
            _service.Add(TestStoreName, p2);

            var result = _service.GetAll()
                                 .Where(x => x.Category == "SameCategory")
                                 .ToList();

            Assert.That(result.Count, Is.EqualTo(2));
        }
    }
}
