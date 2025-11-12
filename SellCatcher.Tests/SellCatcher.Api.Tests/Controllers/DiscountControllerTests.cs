using Microsoft.AspNetCore.Mvc;
using ProductScraper;
using SellCatcher.Api.Controllers;
using SellCatcher.Api.Services;
using NUnit.Framework.Legacy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SellCatcher.Tests.SellCatcher.Api.Tests.Controllers
{
    
    [TestFixture]
    public class DiscountsControllerTests
    {
        private DiscountsController _controller;
        private TestDiscountService _fakeService;

        [SetUp]
        public void Setup()
        {
            _fakeService = new TestDiscountService();
            _controller = new DiscountsController(_fakeService);
        }

        [Test]
        public void GetAll_ReturnsOkWithDiscounts()
        {

            var fakeData = _fakeService.GetAll();
            Assert.That(fakeData.Count(), Is.EqualTo(3));


            var result = _controller.GetAll() as OkObjectResult;
            ClassicAssert.IsNotNull(result);

            var discounts = result.Value as IEnumerable<Product>;
            ClassicAssert.AreEqual(3, discounts.Count());
        }
        
    }
    public class TestDiscountService : DiscountService
    {
        private readonly List<Product> _discounts;

        public TestDiscountService()
        {
            _discounts = new List<Product>
            {
                new Product { Id = 1, Name = "Milk", Price = 20, OldPrice = 30, ValidUntil = DateTime.UtcNow.AddDays(2) },
                new Product { Id = 2, Name = "Bread", Price = 10, OldPrice = 15, ValidUntil = DateTime.UtcNow.AddDays(-1) }, // вже неактивна
                new Product { Id = 3, Name = "Tea", Price = 50, OldPrice = 70, ValidUntil = DateTime.UtcNow.AddDays(1) }
            };
        }

        public new IEnumerable<Product> GetAll() => _discounts;

        public new IEnumerable<Product> GetByStore(int storeId)
        {
            var storeProducts = _discounts.Where(p =>
                (storeId == 1 && p.Name == "Milk") ||
                (storeId == 1 && p.Name == "Bread") ||
                (storeId == 2 && p.Name == "Tea"));
            return storeProducts;
        }

        public new Product GetById(int id)
            => _discounts.FirstOrDefault(d => d.Id == id);

        public new void Add(Product product)
        {
            product.Id = _discounts.Count + 1;
            _discounts.Add(product);
        }
    }
}