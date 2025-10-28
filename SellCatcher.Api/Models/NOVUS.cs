using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace SellCatcher.Api.Models
{
    public class NOVUSProduct
    {
        [BsonId]
        public int Id { get; set; } 
        public string Name { get; set; } = "";  
        public decimal Price { get; set; }  
        public decimal? OldPrice { get; set; }
        public bool IsOnSale { get; set; } = false;
        public string Discount { get; set; } = "";
        public DateTime? ValidUntil { get; set; }
        public string Category { get; set; } = "";
        public DateTime WhenUpdated { get; set; } = DateTime.Now; // this is to know when the price has changed
    }
}
