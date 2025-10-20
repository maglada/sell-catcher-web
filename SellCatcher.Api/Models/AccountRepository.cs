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
        private readonly string _dbPath = @"C:\Users\Admin\Documents\sell-catcher-web\SellCatcher.Api\sellcatcher.db";

        public void Add(Account account)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Account>("accounts");
            var inBase = col.FindOne(a => a.UserName == account.UserName);
            if (inBase != null) {
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
            return col.FindOne(a =>a.UserName == userName);
        }
    }
}