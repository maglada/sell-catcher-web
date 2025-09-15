using LiteDB;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services;

public class LiteDbService
{
    private readonly string _dbPath = "database.db";

    public void InsertItem(Item item)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<Item>("items");
        col.Insert(item);
    }
}
