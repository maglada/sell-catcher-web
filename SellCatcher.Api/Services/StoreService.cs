using DiscApi.Models;

namespace DiscApi.Services
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
    }
}
