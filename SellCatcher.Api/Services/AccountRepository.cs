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
            _col.EnsureIndex(x => x.UserName, unique: true);
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

        public Account? GetByEmail(string email)
        {
            return _col.FindOne(e => e.Email == email);
        }
        public List<Account> GetAll()
        {
            return _col.FindAll().ToList();
        }
        public void Add(Account account)
        {
            if (account.Id == Guid.Empty)
                account.Id = Guid.NewGuid();

            _col.Insert(account);
        }

        public void Update(Account account)
        {
            _col.Update(account);
        }

        public void Delete(Guid id)
        {
            _col.Delete(id);
        }

        public void Dispose() => _db?.Dispose();
    }
}
