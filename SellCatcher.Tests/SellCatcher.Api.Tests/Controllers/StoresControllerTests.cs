using NUnit.Framework;
using NUnit.Framework.Legacy;
using Microsoft.AspNetCore.Mvc;
using SellCatcher.Api.Controllers;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;
using System.Collections.Generic;

namespace SellCatcher.Tests.Controllers
{
    [TestFixture]
    public class StoresControllerTests
    {
        private StoresController _controller;
        private TestStoreService _fakeStoreService;

        [SetUp]
        public void Setup()
        {
            _fakeStoreService = new TestStoreService();
            _controller = new StoresController(_fakeStoreService);
        }

        [Test]
        public void GetAll_ReturnsOkResultWithStores()
        {
            // Act
            var result = _controller.GetAll() as OkObjectResult;

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(200, result.StatusCode);

            var stores = result.Value as List<Store>;
            ClassicAssert.AreEqual(3, stores.Count);
        }

        [Test]
        public void GetById_ExistingStore_ReturnsOk()
        {
            // Act
            var result = _controller.GetById(1) as OkObjectResult;

            // Assert
            ClassicAssert.IsNotNull(result);
            ClassicAssert.AreEqual(200, result.StatusCode);

            var store = result.Value as Store;
            ClassicAssert.AreEqual("АТБ", store.Name);
        }

        [Test]
        public void GetById_NonExistingStore_ReturnsNotFound()
        {
            // Act
            var result = _controller.GetById(999);

            // Assert
            ClassicAssert.IsInstanceOf<NotFoundResult>(result);
        }
    }

    // fake-сервіс для тесту
    public class TestStoreService : StoreService
    {
        private readonly List<Store> _stores = new()
        {
            new Store { Id = 1, Name = "АТБ" },
            new Store { Id = 2, Name = "Novus" },
            new Store { Id = 3, Name = "Сільпо" }
        };

  
        public new IEnumerable<Store> GetAll()
        {
            return _stores;
        }

        public new Store GetById(int id)
        {
            return _stores.Find(s => s.Id == id);
        }
    }
}
