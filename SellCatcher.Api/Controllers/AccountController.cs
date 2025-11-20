using Microsoft.AspNetCore.Mvc;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;

namespace SellCatcher.Api.Controllers

{
    [ApiController]
    [Route("api/accounts")] 
     public class AccountController : ControllerBase
    {
        private readonly AccountRepository _accountService;

        public AccountController(AccountRepository accountService)
        {
            _accountService = accountService;
        }

        [HttpGet] /*Эндпоинт получения всех аккаунтов*/
        public List<Account> GetAll()
        {
            var accounts = _accountService.GetAll();
            return accounts;

        }
        [HttpPost] /*Эндпоинт добавления нового аккаунта*/
        public IActionResult Add([FromBody] Account account)
        {
            if (account == null) return NotFound();
            _accountService.Add(account);
            return CreatedAtAction(nameof(GetAll), new { id = account.Id }, account);
        }

        [HttpGet("{username}")] /*Эндпоинт получения аккаунта по имени пользователя*/
        public IActionResult GetByUsername(string username)
        {
            var account = _accountService.GetByUserName(username);
            if (account == null) return NotFound();
            return Ok(account);
        }
    }
}