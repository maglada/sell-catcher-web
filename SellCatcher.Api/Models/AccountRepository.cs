using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SellCatcher.Api.Models
{
    public class AccountRepository
    {
        private static IDictionary<string, Account> _accounts = new Dictionary<string, Account>();

        public void Add(Account account)
        {
            if (account.UserName == null)
            {
                throw new ArgumentNullException(nameof(account.UserName), "UserName cannot be null.");
            }
            _accounts[account.UserName] = account;
        }
        public Account? GetByUserName(string userName)
        {
            _accounts.TryGetValue(userName, out var account);
            return account;
        }
    }
}