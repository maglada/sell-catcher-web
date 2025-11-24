using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public class StoreService
    {
        private readonly List<Store> _stores = new()
        {
            new Store { Id = 1, Name = "АТБ" },
            new Store { Id = 2, Name = "Novus" },
            new Store { Id = 3, Name = "Сільпо" }
        };

        public IEnumerable<Store> GetAll() => _stores;
        public Store? GetById(int id) => _stores.FirstOrDefault(s => s.Id == id);
        public Store? GetByName(string name) => _stores.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
