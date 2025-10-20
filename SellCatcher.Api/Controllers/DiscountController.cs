using Microsoft.AspNetCore.Mvc;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;

// DiscountService не добавлял, потому-что Виталик сказал что не нужно. Но если нужно, могу добавить. 

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
        public IActionResult GetAll()
        {
            return Ok(_discountService.GetAll());
        }

        [HttpGet("store/{storeId}")] /*Эндпоинт получения скидок по магазину*/
        public IActionResult GetByStore(int storeId)
        {
            return Ok(_discountService.GetByStore(storeId));
        }

        [HttpGet("{id}")] /*Эндпоинт получения скидки по ID*/
        public IActionResult GetById(int id)
        {
            var discount = _discountService.GetById(id);
            if (discount == null) return NotFound();
            return Ok(discount);
        }

        [HttpPost] /*Эндпоинт добавления новой скидки*/
        public IActionResult Add(Discount discount)
        {
            _discountService.Add(discount);
            return CreatedAtAction(nameof(GetById), new { id = discount.Id }, discount);
        }
        [HttpGet("active")] /*Эндпоинт получения активных скидок*/
        public IActionResult GetActiveDiscounts()
        {
            var active = _discountService
                .GetAll()
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
            var topDeals = _discountService
                .GetAll()
                .OrderByDescending(d => d.OldPrice - d.NewPrice)
                .Take(3); 
            return Ok(topDeals);
        }
    }
}