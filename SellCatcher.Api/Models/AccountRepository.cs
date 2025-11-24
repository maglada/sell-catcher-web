using LiteDB;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public class AccountRepository : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<Account> _col;

        public AccountRepository(string dbPath = "sellcatcher.db")
        {
            _db = new LiteDatabase(dbPath);
            _col = _db.GetCollection<Account>("accounts");
            // Используем автоинкремент для Id
            _col.EnsureIndex(x => x.Id);
            _col.EnsureIndex(x => x.UserName, true);
        }

        public Account? GetByUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return null;
            return _col.FindOne(x => x.UserName == userName);
        }

        public Account? GetById(Guid id)
        {
            return _col.FindById(id);
        }

        public void Add(Account account)
        {
            // Если Id == 0, LiteDB поставит новый авто-id
            _col.Insert(account);
        }

        public void Update(Account account)
        {
            _col.Update(account);
        }

        public void Delete(int id)
        {
            _col.Delete(id);
        }

        public void Dispose() => _db?.Dispose();
    }
}
