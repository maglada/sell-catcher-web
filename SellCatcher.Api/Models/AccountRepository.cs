using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.AspNetCore.Identity;

namespace SellCatcher.Api.Models
{
    public class AccountRepository
    {
        private readonly string _dbPath;

        public AccountRepository()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var DBPlace = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));

            _dbPath = Path.Combine(DBPlace, "sellcatcher.db");
        }

        public void Add(Account account)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Account>("accounts");
            var inBase = col.FindOne(a => a.UserName == account.UserName);
            if (inBase != null)
            {
                account.Id = inBase.Id;
                col.Update(account);
            }
            else
            {
                col.Insert(account);
            }
        }
        public Account? GetByUserName(string userName)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Account>("accounts");
            return col.FindOne(a => a.UserName == userName);
        }
        public List<Account> GetAll(){
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Account>("accounts");
            return col.FindAll().ToList();
        }
    }
}
      