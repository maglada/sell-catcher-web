using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using SellCatcher.Api.Models;


namespace SellCatcher.Api.Services
{
    public class DiscountService
    {
        private readonly string _dbPath = @"C:\Users\Admin\Documents\sell-catcher-web\SellCatcher.Api\sellcatcher.db";

        public void Add(Discount discount)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Discount>("discounts");
            col.Insert(discount);
        }

        public List<Discount> GetAll()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<Discount>("discounts").FindAll().ToList();
        }

        public List<Discount> GetByStore(int storeId)
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<Discount>("discounts").Find(d => d.StoreId == storeId).ToList();
        }

        public Discount GetById(int id)
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<Discount>("discounts").FindById(id);
        }
    }
}
