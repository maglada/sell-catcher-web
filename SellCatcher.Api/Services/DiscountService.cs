<<<<<<< HEAD
using SellCatcher.Api.Models;

=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using SellCatcher.Api.Models;


>>>>>>> 33463db23ef59ab331b3163b9bba868a49172fc9
namespace SellCatcher.Api.Services
{
    public class DiscountService
    {
<<<<<<< HEAD
        private readonly List<Discount> _discounts = new()
        {
            new Discount { Id = 1, StoreId = 1, Product = "Stella Artois 0.5л", OldPrice = 35, NewPrice = 29.5m, ValidUntil = DateTime.UtcNow.AddDays(7) },
            new Discount { Id = 2, StoreId = 2, Product = "Хліб білий", OldPrice = 20, NewPrice = 15, ValidUntil = DateTime.UtcNow.AddDays(5) },
            new Discount { Id = 3, StoreId = 3, Product = "Козацька рада 0.5л", OldPrice = 80, NewPrice = 65, ValidUntil = DateTime.UtcNow.AddDays(10) }
        };

        public IEnumerable<Discount> GetAll() => _discounts;

        public IEnumerable<Discount> GetByStore(int storeId) =>
            _discounts.Where(d => d.StoreId == storeId);

        public Discount? GetById(int id) => _discounts.FirstOrDefault(d => d.Id == id);

        public void Add(Discount discount)
        {
            discount.Id = _discounts.Max(d => d.Id) + 1;
            _discounts.Add(discount);
=======
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
>>>>>>> 33463db23ef59ab331b3163b9bba868a49172fc9
        }
    }
}
