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
        private readonly string _dbPath = @"C:\Users\Admin\Documents\sell-catcher-web\SellCatcher.Api\sellcatcher.db";

        public void AddAtbProduct(ATBProduct p)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<ATBProduct>("atb");
            col.Insert(p);
        }

        public void AddNovusProduct(NOVUSProduct p)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<NOVUSProduct>("novus");
            col.Insert(p);

            Console.WriteLine($"💾 Добавлен товар: {p.Name} — {p.Price} ₴");
        }

        public List<ATBProduct> GetAtbProducts()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<ATBProduct>("atb").FindAll().ToList();
        }

        public List<NOVUSProduct> GetNovusProducts()
        {
            using var db = new LiteDatabase(_dbPath);
            var products = db.GetCollection<NOVUSProduct>("novus").FindAll().ToList();

            Console.WriteLine($"📦 В базе найдено {products.Count} товаров Novus.");
            return products;
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