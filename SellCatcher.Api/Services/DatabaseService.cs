using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prices.db");

        public void AddShop1Product(Shop1Product p)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Shop1Product>("shop1");
            col.Insert(p);
        }

        public void AddShop2Product(Shop2Product p)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Shop2Product>("shop2");
            col.Insert(p);
        }

        public List<Shop1Product> GetShop1Products()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<Shop1Product>("shop1").FindAll().ToList();
        }

        public List<Shop2Product> GetShop2Products()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<Shop2Product>("shop2").FindAll().ToList();
        }

        public void SaveComparison(Comparison c)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Comparison>("comparisons");
            col.Insert(c);
        }

        public List<Comparison> GetLastComparisons(int count)
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<Comparison>("comparisons").FindAll().OrderByDescending(x => x.Date).Take(count).ToList();
        }
    }
}