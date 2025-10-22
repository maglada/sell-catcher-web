using Microsoft.AspNetCore.Mvc;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;

namespace SellCatcher.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] /*Эндпоинт сравнения*/
    public class ComparisonController : ControllerBase
    {
        private readonly DiscountService _discountService;
        private readonly StoreService _storeService;

        public ComparisonController(DiscountService discountService, StoreService storeService)
        {
            _discountService = discountService;
            _storeService = storeService;
        }

        [HttpGet("stores")] /*Эндпоинт сравнения магазинов по активным скидкам*/
        public IActionResult CompareStores()
        {
            var stores = _storeService.GetAll();
            var result = stores.Select(store => new
            {
                Store = store.Name,
                ActiveDiscounts = _discountService
                    .GetByStore(store.Id)
                    .Count(d => d.ValidUntil >= DateTime.UtcNow)
            });

            return Ok(result);
        }

        [HttpGet("product/{productName}")] /*Эндпоинт сравнения цен на товар*/
        public IActionResult CompareProduct(string productName)
        {
            var discounts = _discountService
                .GetAll()
                .Where(d => d.Product.Contains(productName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.NewPrice);

            if (!discounts.Any())
                return NotFound(new { Message = "Знижки на цей товар не знайдено." });

            return Ok(discounts);
        }
    }
}