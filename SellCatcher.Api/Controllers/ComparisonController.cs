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
                .Where(d => d.Name.Contains(productName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.Price);

            if (!discounts.Any())
                return NotFound(new { Message = "Знижки на цей товар не знайдено." });

            return Ok(discounts);
        }

        // BAD SOLUTION BELOW - TO REFACTOR LATER
        //TOTEST POSTMAN MANUALLY
        //categ filter    
        [HttpGet("filter/{category}")] 
        public IActionResult FilterByCategory(string category)
        {
            var discounts = _discountService
                .GetAll()
                .Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

            if (!discounts.Any())
                return NotFound(new { Message = "Знижки в цій категорії не знайдено." });

            return Ok(discounts);
        }

        //name filter
        [HttpGet("filter/sotre/{storeName}")] 
        public IActionResult FilterByStore(string storeName)
        {
            var discounts = _discountService
                .GetAll()
                .Where(d => d.Name.Equals(storeName, StringComparison.OrdinalIgnoreCase));
            var store = _storeService.GetByName(storeName);
            if (store == null)
                return NotFound(new { Message = "Магазин не знайдено." });
            return Ok(store);
        }

        //minmax filter
        [HttpGet("filter/price/{minPrice}/{maxPrice}")]
        public IActionResult FilterByPriceRange(decimal minPrice, decimal maxPrice)
        {
            var discounts = _discountService
                .GetAll()
                .Where(d => d.Price >= minPrice && d.Price <= maxPrice);
            if (!discounts.Any()) return NotFound(new { Message = "Знижки в цьому ціновому діапазоні не знайдено." });
            return Ok(discounts);    
        }

        //keyword filter
        [HttpGet("filter/search/{searchTerm}")]
        public IActionResult SearchDiscounts(string searchTerm)
        {
            var discounts = _discountService
                .GetAll()
                .Where(d => d.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            if (!discounts.Any())
                return NotFound(new { Message = "Знижки за цим запитом не знайдено." });
            return Ok(discounts);
        }
    }
}