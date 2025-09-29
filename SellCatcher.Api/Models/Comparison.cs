using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace SellCatcher.Api.Models
{
    public class Comparison
    {
        [BsonId]
        public int Id { get; set; }
        public string ProductName { get; set; } = "";  
        public decimal Shop1Price { get; set; }  
        public decimal Shop2Price { get; set; }  
        public string CheaperIn { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
