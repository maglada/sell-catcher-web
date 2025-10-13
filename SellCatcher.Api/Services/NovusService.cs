using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public class NovusService
    {
        private readonly string _dbPath = @"C:\Users\Admin\Documents\sell-catcher-web\SellCatcher.Api\sellcatcher.db";
        public async Task NovusScraperToDB()
        {
            var (products, discounts) = await Scraper.RunScraper();

            using (var db = new LiteDatabase(_dbPath))
            {
                var novusCollection = db.GetCollection<NOVUSProduct>("novus");
                novusCollection.InsertBulk(products);
                Console.WriteLine($"Saved {products.Count} products in DB.");

                var discountCollection = db.GetCollection<Discount>("discounts");
                discountCollection.InsertBulk(discounts);
                Console.WriteLine($"Saved {discounts.Count} discounts in DB.");
            }
        }
    }
}
