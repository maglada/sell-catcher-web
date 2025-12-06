using Microsoft.AspNetCore.Mvc;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;
using ProductScraper;

namespace SellCatcher.Api.Controllers

{
    [ApiController]
    [Route("api/[controller]")] /*Эндпоинт скидок*/
    public class DiscountsController : ControllerBase
    {
        private readonly DiscountService _discountService;

        public DiscountsController(DiscountService discountService)
        {
            _discountService = discountService;
        }

        [HttpGet] /*Эндпоинт получения всех скидок*/
        public IActionResult GetAllSales()
        {
           var discounts = _discountService.GetAllSales();
           return Ok(discounts);
        }

        [HttpGet("store/{storeId}")] /*Эндпоинт получения скидок по магазину*/
        public IActionResult GetByStore(int storeId)
        {
           var discounts = _discountService.GetByStore(storeId);
           return Ok(discounts);
        }

        [HttpGet("{id:guid}")] /*Эндпоинт получения скидки по ID*/
        public IActionResult GetById(Guid id)
        {
           var discount = _discountService.GetById(id);
           if (discount == null) return NotFound();
           return Ok(discount);
        }

        [HttpGet("active")] /*Эндпоинт получения активных скидок*/
        public IActionResult GetActiveDiscounts()
        {
           var active = _discountService
               .GetAllSales()
               .Where(d => d.ValidUntil >= DateTime.UtcNow);
           return Ok(active);
        }
        [HttpGet("store/{storeId}/active")] /*Эндпоинт получения активных скидок по магазину*/
        public IActionResult GetDiscountsStore(int storeId)
        {
           var discounts = _discountService
               .GetByStore(storeId)
               .Where(d => d.ValidUntil >= DateTime.UtcNow);
           return Ok(discounts);
        }
        [HttpGet("top")] /*Эндпоинт получения лучших скидок*/
        public IActionResult GetTopDeals()
        {
           var topDeals = _discountService.GetAllSales()
               .OrderByDescending(d => d.OldPrice - d.Price)
               .Take(2);
           return Ok(topDeals);
        }
         [HttpGet("products")] /*Эндпоинт получения всех товаров*/
        public IActionResult GetAllProducts()
        {
            var products = _discountService.GetAllProducts();
            return Ok(products);
        }
        [HttpGet("search")] /*Эндпоинт поиска скидок*/
        public IActionResult Search([FromQuery] string query)
        {
            var results = _discountService.Search(query);
            return Ok(results);
        }

    }
}
