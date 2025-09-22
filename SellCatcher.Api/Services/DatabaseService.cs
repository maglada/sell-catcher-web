using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using PriceComparisonApp.Models;

namespace PriceComparisonApp.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prices.db");

        // Добавить товар в Shop1
        public void AddShop1Product(Shop1Product product)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Shop1Product>("shop1products");
            col.Insert(product);
        }

        // Добавить товар в Shop2
        public void AddShop2Product(Shop2Product product)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Shop2Product>("shop2products");
            col.Insert(product);
        }

        // Получить все товары
        public List<Shop1Product> GetShop1Products()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<Shop1Product>("shop1products").FindAll().ToList();
        }

        public List<Shop2Product> GetShop2Products()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<Shop2Product>("shop2products").FindAll().ToList();
        }

        // Сохранить результат сравнения
        public void SaveComparison(Comparison comparison)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Comparison>("comparisons");
            col.Insert(comparison);
        }

        // Получить последние N сравнений
        public List<Comparison> GetLastComparisons(int count)
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<Comparison>("comparisons")
                     .FindAll()
                     .OrderByDescending(c => c.Date)
                     .Take(count)
                     .ToList();
        }
    }
}