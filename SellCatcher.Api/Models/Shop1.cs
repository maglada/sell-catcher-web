using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SellCatcher.Api.Models
{
    public class Shop1Product
    {
        [BsonId]
        public int Id { get; set; } 
        public string Name { get; set; } = "";  
        public decimal Price { get; set; } 
    }
}
