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

        public void LoadToDatabase(System.Collections.Generic.List<ATBProduct> atb, System.Collections.Generic.List<NOVUSProduct> novus)
        {
            foreach (var p in atb) _db.AddAtbProduct(p);
            foreach (var p in novus) _db.AddNovusProduct(p);
        }

        public void CompareAllAndSave()
        {
            var s1 = _db.GetAtbProducts();
            var s2 = _db.GetNovusProducts();

            foreach (var p1 in s1)
            {
                var p2 = s2.FirstOrDefault(x => x.Name == p1.Name);
                if (p2 == null) continue;

                var cheaper = p1.Price < p2.Price ? "atb" : p1.Price > p2.Price ? "novus" : "Equal";

                var comp = new Comparison
                {
                    ProductName = p1.Name,
                    ATBPrice = p1.Price,
                    NOVUSPrice = p2.Price,
                    CheaperIn = cheaper,
                    Date = DateTime.UtcNow
                };

                _db.SaveComparison(comp);

            }
        }
    }
}
