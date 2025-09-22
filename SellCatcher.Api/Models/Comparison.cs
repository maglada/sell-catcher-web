using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceComparisonApp.Models
{
    public class Comparison
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = "";  // Название товара
        public decimal Shop1Price { get; set; }  // Цена в магазине 1
        public decimal Shop2Price { get; set; }  // Цена в магазине 2
        public string CheaperIn { get; set; } = "";   // Где дешевле
        public DateTime Date { get; set; }      // Когда сравнивали
    }
}
