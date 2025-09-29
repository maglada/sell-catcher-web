using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SellCatcher.Api.Services;
using SellCatcher.Api.Models;
using SellCatcher.Api.DTOs; // <-- fix here

namespace SellCatcher.Api.Controllers;

[ApiController]
[Route("api")] // <-- change here
public class AuthController : ControllerBase
{
    private readonly AccountService accountService;

    public AuthController(AccountService accountService)
    {
        this.accountService = accountService;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequestDto request)
    {
        accountService.Register(request.UserName, request.FirstName, request.LastName, request.Password);
        return Ok(new { Message = "Registration successful" });
    }
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        var token = accountService.Login(request.UserName, request.Password);
        if (token == null)
        {
            return Unauthorized();
        }
        return Ok(new { Message = "Login successful", Token = token });
    }
}