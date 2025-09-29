using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public class PriceService
    {
        private readonly DatabaseService _db;

        public PriceService(DatabaseService db)
        {
            _db = db;
        }

        public void LoadToDatabase(System.Collections.Generic.List<Shop1Product> shop1, System.Collections.Generic.List<Shop2Product> shop2)
        {
            foreach (var p in shop1) _db.AddShop1Product(p);
            foreach (var p in shop2) _db.AddShop2Product(p);
        }

        public void CompareAllAndSave()
        {
            var s1 = _db.GetShop1Products();
            var s2 = _db.GetShop2Products();

            foreach (var p1 in s1)
            {
                var p2 = s2.FirstOrDefault(x => x.Name == p1.Name);
                if (p2 == null) continue;

                var cheaper = p1.Price < p2.Price ? "Shop1" : p1.Price > p2.Price ? "Shop2" : "Equal";

                var comp = new Comparison
                {
                    ProductName = p1.Name,
                    Shop1Price = p1.Price,
                    Shop2Price = p2.Price,
                    CheaperIn = cheaper,
                    Date = DateTime.UtcNow
                };

                _db.SaveComparison(comp);

            }
        }
    }
}
