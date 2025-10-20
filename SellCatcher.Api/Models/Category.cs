using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace SellCatcher.Api.Models
{
    [BsonId]
    public class Category
    {
        public string Name { get; set; } = "";
        public List<NOVUSProduct> Products { get; set; } = new();
    }
}
