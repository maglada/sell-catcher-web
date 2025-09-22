using Microsoft.AspNetCore.Mvc;
using DiscApi.Models;
using DiscApi.Services;

// DiscountService не добавлял, потому-что Виталик сказал что не нужно. Но если нужно, могу добавить. 

namespace DiscApi.Controllers
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
            return Ok(_discountService.GetAll());
        }

        [HttpGet("store/{storeId}")]
        public IActionResult GetByStore(int storeId)
        {
            return Ok(_discountService.GetByStore(storeId));
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var discount = _discountService.GetById(id);
            if (discount == null) return NotFound();
            return Ok(discount);
        }

        [HttpPost]
        public IActionResult Add(Discount discount)
        {
            _discountService.Add(discount);
            return CreatedAtAction(nameof(GetById), new { id = discount.Id }, discount);
        }
    }
}
