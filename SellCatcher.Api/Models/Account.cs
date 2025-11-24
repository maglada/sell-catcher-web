using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace SellCatcher.Api.Models
{
    public class Account
    {
        [BsonId]
        public Guid Id { get; set; } 
        public string UserName { get; set; }
        public string Email { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordHash { get; set; }
    }
}