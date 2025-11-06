using Microsoft.AspNetCore.Identity;
using SellCatcher.Api.Models;
using SellCatcher.Api.DTOs.User;

namespace SellCatcher.Api.Services
{
    public class AccountService
    {
        private readonly AccountRepository _accountRepository;

        public AccountService(AccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public void Register(string? userName, string? firstName, string? lastName, string password)
        {
            var account = new Account
            {
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                PasswordHash = string.Empty
            };
            var passwordHasher = new PasswordHasher<Account>();
            account.PasswordHash = passwordHasher.HashPassword(account, password);

            _accountRepository.Add(account);
        }

        public string Login(string userName, string password)
        {
            var account = _accountRepository.GetByUserName(userName);
            if (account == null) throw new Exception("Unauthorized");

            var passwordHasher = new PasswordHasher<Account>();
            if (string.IsNullOrEmpty(account.PasswordHash)) throw new Exception("Unauthorized");

            var result = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
            if (result == PasswordVerificationResult.Success) return "";
            throw new Exception("Unauthorized");
        }

        public Account? ValidateCredentialsAndGetAccount(string userName, string password)
        {
            var account = _accountRepository.GetByUserName(userName);
            if (account == null) return null;

            var passwordHasher = new PasswordHasher<Account>();
            var result = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
            return result == PasswordVerificationResult.Success ? account : null;
        }
    }
}
