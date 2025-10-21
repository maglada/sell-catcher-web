using Microsoft.AspNetCore.Mvc;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;

namespace SellCatcher.Api.Controllers

{
    [ApiController]
    [Route("api/[controller]")]
    public class DiscountsController : ControllerBase
    {
        private readonly DiscountService _discountService;

        public DiscountsController(DiscountService discountService)
        {
            _discountService = discountService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var discounts = _discountService.GetAll();
            if (discounts == null) return NotFound();
            return Ok(discounts);
        }

        [HttpGet("store/{storeId}")]
        public IActionResult GetByStore(int storeId)
        {
            var discounts = _discountService.GetByStore(storeId);
            if (discounts == null) return NotFound();
            return Ok(discounts);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var discount = _discountService.GetById(id);
            if (discount == null) return NotFound();
            return Ok(discount);
        }

        [HttpPost]
        public IActionResult Add([FromBody] NOVUSProduct discount)
        {
            if (discount == null) return NotFound();
            _discountService.Add(discount);
            return CreatedAtAction(nameof(GetById), new { id = discount.Id }, discount);
        }
    }
}
