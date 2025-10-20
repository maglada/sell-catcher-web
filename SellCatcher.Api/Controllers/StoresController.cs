using Microsoft.AspNetCore.Mvc;
using SellCatcher.Api .Services;

namespace  SellCatcher.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] /*Эндпоинт магазинов*/
    public class StoresController : ControllerBase
    {
        private readonly StoreService _storeService;

        public StoresController(StoreService storeService)
        {
            _storeService = storeService;
        }

        [HttpGet] /*Эндпоинт получения всех магазинов*/
        public IActionResult GetAll()
        {
            return Ok(_storeService.GetAll());
        }

        [HttpGet("{id}")] /*Эндпоинт получения магазина по ID*/
        public IActionResult GetById(int id)
        {
            var store = _storeService.GetById(id);
            if (store == null) return NotFound();
            return Ok(store);
        }
    }
}
