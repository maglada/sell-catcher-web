//using Microsoft.AspNetCore.Mvc;
//using NUnit.Framework;
//using ProductScraper;
//using SellCatcher.Api.Controllers;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using ProductScraper;
//using SellCatcher.Api.Services;
//using System;
//using System.Collections.Generic;
//using System.Linq;



//namespace SellCatcher.Tests.Controllers
//{

//    [TestFixture]
//    public class DiscountsControllerTests
//    {
//        private FakeDiscountService _fakeService;
//        private DiscountsController _controller;

//        private List<Product> _testProducts;
//        private Guid _productId1;
//        private Guid _productId2;
//        private Guid _productId3;

//        [SetUp]
//        public void Setup()
//        {
//            _productId1 = Guid.NewGuid();
//            _productId2 = Guid.NewGuid();
//            _productId3 = Guid.NewGuid();

//            _testProducts = new List<Product>
//            {
//                new Product
//                {
//                    Id = _productId1,
//                    Name = "Milk",
//                    Price = 20,
//                    OldPrice = 30,
//                    IsOnSale = true,
//                    ValidUntil = DateTime.UtcNow.AddDays(2),
//                    Category = "Dairy",
//                    StoreId = 1
//                },
//                new Product
//                {
//                    Id = _productId2,
//                    Name = "Bread",
//                    Price = 10,
//                    OldPrice = 15,
//                    IsOnSale = true,
//                    ValidUntil = DateTime.UtcNow.AddDays(-1),
//                    Category = "Bakery",
//                    StoreId = 1
//                },
//                new Product
//                {
//                    Id = _productId3,
//                    Name = "Tea",
//                    Price = 50,
//                    OldPrice = 70,
//                    IsOnSale = true,
//                    ValidUntil = DateTime.UtcNow.AddDays(1),
//                    Category = "Beverages",
//                    StoreId = 2
//                }
//            };

//            _fakeService = new FakeDiscountService
//            {
//                Products = _testProducts
//            };

//            _controller = new DiscountsController(_fakeService);
//        }

//        // =================== GETALL ===================

//        [Test]
//        public void GetAll_ReturnsAllProducts()
//        {
//            var result = _controller.GetAll() as OkObjectResult;

//            Assert.That(result, Is.Not.Null);
//            var products = result.Value as IEnumerable<Product>;
//            Assert.That(products.Count(), Is.EqualTo(3));
//        }

//        // =================== GET BY STORE ===================

//        [Test]
//        public void GetByStore_Store1_Returns2Products()
//        {
//            var result = _controller.GetByStore(1) as OkObjectResult;

//            Assert.That(result, Is.Not.Null);
//            var p = result.Value as IEnumerable<Product>;

//            Assert.That(p.Count(), Is.EqualTo(2));
//        }

//        [Test]
//        public void GetByStore_Store2_Returns1Product()
//        {
//            var result = _controller.GetByStore(2) as OkObjectResult;

//            Assert.That(result, Is.Not.Null);
//            var p = result.Value as IEnumerable<Product>;

//            Assert.That(p.Count(), Is.EqualTo(1));
//            Assert.That(p.First().Name, Is.EqualTo("Tea"));
//        }

//        // =================== GET BY ID ===================

//        [Test]
//        public void GetById_ValidId_ReturnsProduct()
//        {
//            var result = _controller.GetById(_productId1) as OkObjectResult;

//            Assert.That(result, Is.Not.Null);
//            var product = result.Value as Product;

//            Assert.That(product.Id, Is.EqualTo(_productId1));
//        }

//        [Test]
//        public void GetById_InvalidId_ReturnsNotFound()
//        {
//            var result = _controller.GetById(Guid.NewGuid());

//            Assert.That(result, Is.InstanceOf<NotFoundResult>());
//        }

//        // =================== ACTIVE DISCOUNTS ===================

//        [Test]
//        public void GetActiveDiscounts_ReturnsOnlyActive()
//        {
//            var result = _controller.GetActiveDiscounts() as OkObjectResult;
//            var list = result.Value as IEnumerable<Product>;

//            Assert.That(list.Count(), Is.EqualTo(2));
//            Assert.That(list.Any(p => p.Name == "Bread"), Is.False);
//        }

//        // =================== STORE ACTIVE DISCOUNTS ===================

//        [Test]
//        public void GetDiscountsStore_ActiveOnly()
//        {
//            var result = _controller.GetDiscountsStore(1) as OkObjectResult;
//            var list = result.Value as IEnumerable<Product>;

//            Assert.That(list.Count(), Is.EqualTo(1));
//            Assert.That(list.First().Name, Is.EqualTo("Milk"));
//        }

//        // =================== TOP DEALS ===================

//        [Test]
//        public void GetTopDeals_ReturnsTop2()
//        {
//            var result = _controller.GetTopDeals() as OkObjectResult;
//            var list = (result.Value as IEnumerable<Product>).ToList();

//            Assert.That(list.Count, Is.EqualTo(2));
//            Assert.That(list[0].Name, Is.EqualTo("Tea")); // max discount
//            Assert.That(list[1].Name, Is.EqualTo("Milk"));
//        }

//        // =================== EDGE CASES ===================

//        [Test]
//        public void GetAll_EmptyList_ReturnsEmpty()
//        {
//            _fakeService.Products = new List<Product>();

//            var result = _controller.GetAll() as OkObjectResult;
//            var p = result.Value as IEnumerable<Product>;

//            Assert.That(p, Is.Empty);
//        }

//        [Test]
//        public void GetActiveDiscounts_AllExpired_ReturnsEmpty()
//        {
//            _fakeService.Products = new List<Product>
//            {
//                new Product { ValidUntil = DateTime.UtcNow.AddDays(-1) },
//                new Product { ValidUntil = DateTime.UtcNow.AddDays(-2) }
//            };

//            var result = _controller.GetActiveDiscounts() as OkObjectResult;
//            var p = result.Value as IEnumerable<Product>;

//            Assert.That(p, Is.Empty);
//        }
//    }
//}
