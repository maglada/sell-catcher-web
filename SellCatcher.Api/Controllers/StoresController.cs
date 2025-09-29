using Microsoft.AspNetCore.Mvc;
using DiscApi.Services;

namespace DiscApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoresController : ControllerBase
    {
        private readonly StoreService _storeService;

        public StoresController(StoreService storeService)
        {
            _storeService = storeService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_storeService.GetAll());
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var store = _storeService.GetById(id);
            if (store == null) return NotFound();
            return Ok(store);
        }
    }
}
