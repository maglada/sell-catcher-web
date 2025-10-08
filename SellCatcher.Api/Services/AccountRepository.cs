using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public class AccountRepository
    {
        private readonly string _dbPath = @"C:\Users\Admin\Documents\sell-catcher-web\SellCatcher.Api\sellcatcher.db";

        public void Add(User user)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<User>("users");
            col.Insert(user);
        }

        public User? GetById(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<User>("users").FindById(id);
        }

        public IEnumerable<User> GetAll()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<User>("users").FindAll().ToList();
        }

        public void Update(User user)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<User>("users");
            col.Update(user);
        }

        public void Delete(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<User>("users");
            col.Delete(id);
        }
    }
}
