using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace ProductScraper
{
    public class Product
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();  
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public string Discount { get; set; }
        public DateTime? ValidUntil { get; set; }
        public bool IsOnSale { get; set; }
        public decimal? BulkPrice { get; set; } = null;
        public bool IsBulk { get; set; } = false;
        public string StoreName {  get; set; } 
        public string Category { get; set; }
        public DateTime WhenUpdated { get; set; } = DateTime.Now; // this is to know when the price has changed
    }
}