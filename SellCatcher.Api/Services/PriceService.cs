using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PriceComparisonApp.Shops;

namespace PriceComparisonApp.Services
{
    public class PriceService
    {
        private readonly DatabaseService _dbService;

        public PriceService(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // Сравнить все товары по названиям
        public void CompareAllProducts()
        {
            var shop1Products = _dbService.GetShop1Products();
            var shop2Products = _dbService.GetShop2Products();

            foreach (var p1 in shop1Products)
            {
                var p2 = shop2Products.FirstOrDefault(x => x.Name == p1.Name);
                if (p2 != null)
                {
                    var cheaperIn = p1.Price < p2.Price ? "Shop1" :
                                    p1.Price > p2.Price ? "Shop2" : "Equal";

                    var comparison = new Comparison
                    {
                        ProductName = p1.Name,
                        Shop1Price = p1.Price,
                        Shop2Price = p2.Price,
                        CheaperIn = cheaperIn,
                        Date = DateTime.Now
                    };

                    _dbService.SaveComparison(comparison);
                }
            }
        }
    }
}
