using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SellCatcher.Api.Models;
using SellCatcher.Api.DTOs;

namespace SellCatcher.Api.Services
{
    public class AccountService(AccountRepository accountRepository, JWTService jwtService)
    {
        public void Register(string? userName, string? firstName, string? lastName, string password)
        {
            // Hash the password (for simplicity, using plain text here; use a proper hashing algorithm in production)
            var passwordHash = password; // Replace with actual hashing
            var account = new Account 
            {
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                PasswordHash = passwordHash
            };
            var passwordHasher = new PasswordHasher<Account>();
            account.PasswordHash = passwordHasher.HashPassword(account, password);

            accountRepository.Add(account);
        }
        public string Login(string userName, string password)
        {
            var account = accountRepository.GetByUserName(userName);
            if (account == null)
            {
                throw new Exception("Unauthorized");
            }

            var passwordHasher = new PasswordHasher<Account>();
            if (string.IsNullOrEmpty(account.PasswordHash))
            {
                throw new Exception("Unauthorized");
            }
            var result = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
            if (result == PasswordVerificationResult.Success)
            {
                return jwtService.GenerateToken(account);  // generate JWT token
            }
            else
            {
                throw new Exception("Unauthorized");
            }
        }
    }
}