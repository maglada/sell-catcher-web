using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace SellCatcher.Api.Models
{
    public class Shop2Product
    {
        [BsonId]
        public int Id { get; set; }
        public string Name { get; set; } = "";  
        public decimal Price { get; set; }  
    }
}