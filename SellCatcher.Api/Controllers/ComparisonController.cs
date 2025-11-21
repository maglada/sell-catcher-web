using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
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
/*        //categ filter    
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
*/

        [HttpGet("filter")]
        public IActionResult Filtering()  //will look like filter?category=x. CAN BE COMBINED like filter?category=x&minPrice=y and so on
        {
            var discounts = _discountService.GetAll().AsQueryable();
            
            // categ filter (?category=x)
            if (Request.Query.TryGetValue("category", out var category))
            {
                discounts = discounts.Where(d => 
                    d.Category.Equals(category.ToString(), StringComparison.OrdinalIgnoreCase));
            }
            
            // name filter (?storeName=x)
            if (Request.Query.TryGetValue("storeName", out var storeName))
            {
                discounts = discounts.Where(d => 
                    d.Name.Equals(storeName.ToString(), StringComparison.OrdinalIgnoreCase));
            }
            
            // min filter (?minPrice=x)
            if (Request.Query.TryGetValue("minPrice", out var minPriceStr) 
                && decimal.TryParse(minPriceStr, out var minPrice))
            {
                discounts = discounts.Where(d => d.Price >= minPrice);
            }
            
            // max filter (?maxPrice=x)
            if (Request.Query.TryGetValue("maxPrice", out var maxPriceStr) 
                && decimal.TryParse(maxPriceStr, out var maxPrice))
            {
                discounts = discounts.Where(d => d.Price <= maxPrice);
            }
            // BOT ABOVE CAN BE SET TO DIFF SLIDERS(two sliders in frontend)
            
            // search filter (?search=x)
            if (Request.Query.TryGetValue("search", out var searchTerm))
            {
                discounts = discounts.Where(d => 
                    d.Name.Contains(searchTerm.ToString(), StringComparison.OrdinalIgnoreCase));
            }
            
            // fin. this one is what user gets(thats why ?param1=x&param2=y is possible)
            var result = discounts.ToList();
            
            if (!result.Any())
                return NotFound(new { Message = "Знижки за заданими критеріями не знайдено." });
            
            return Ok(result);
        }
        
    }
}