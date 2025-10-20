using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SellCatcher.Api.Models
{
    public class Discount
    {
        [BsonId]
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string Product { get; set; } = ""; 
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public DateTime ValidUntil { get; set; }
    }
}
