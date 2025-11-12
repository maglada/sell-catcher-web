using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/***
При помилці шляху бази даних потрібно змінити шлях в AccountRepository.cs
private readonly string _dbPath;

        public AccountRepository()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var DBPlace = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));

            _dbPath = Path.Combine(DBPlace, "sellcatcher.db");
        }

***/

namespace SellCatcher.Tests.SellCatcher.Api.Tests.Controllers
{
    [TestFixture]
    public class ApiAuthControllerTests
    {
        private AccountRepository accountRepository;
        private JWTService jwtService;
        private AccountService accountService;
        [SetUp]
        public void Setup()
        {
            // Встановлюємо JWT секретний ключ для тестів
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "this-is-a-test-secret-key-with-at-least-32-characters-long");

            // Ініціалізуємо сервіси
            accountRepository = new AccountRepository();
            jwtService = new JWTService();
            accountService = new AccountService(accountRepository, jwtService);
        }




        //Тест на правильний пароль 
        [Test]
        public void Login_WithTheCorrectPasswordForRealUser_ReturnsTokenForCorrectUsername()
        {
            // Arrange
            string expectedUsername = "testusername";
            string password = "testpassword";
            
            Account testAccount = new Account
            {
                UserName = expectedUsername,
                FirstName = "Test",
                LastName = "User"
            };

            var passwordHasher = new PasswordHasher<Account>();
            testAccount.PasswordHash = passwordHasher.HashPassword(testAccount, password);

            accountRepository.Add(testAccount);  

            // Act
            string token = accountService.Login(expectedUsername, password);

            // Assert
            Assert.That(token, Is.Not.Null, "Token should not be null for valid credentials");
            Assert.That(token, Is.Not.Empty, "Token should not be empty");

       }




        //Тест на НЕ правильний пароль
        [Test]
        public void Login_WithTheINCorrectPasswordForRealUser_ReturnsError()
        {
            // Arrange
            string expectedUsername = "testusername";
            string password = "testpassword";
            string wrongPassword = "wrongpassword";
            
            Account testAccount = new Account
            {
                UserName = expectedUsername,
                FirstName = "Test",
                LastName = "User"
            };

            var passwordHasher = new PasswordHasher<Account>();
            testAccount.PasswordHash = passwordHasher.HashPassword(testAccount, password);

            accountRepository.Add(testAccount);

            // Act
            var ex = Assert.Throws<Exception>(() => accountService.Login(expectedUsername, wrongPassword));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("Unauthorized"));//ХЕШЕР Паролів повертає "Unauthorized"

        }



        //тест на неіснуючого юзера
        [Test]
        public void Login_WithTheINCorrectUser()
        {
            // Arrange
            string expectedUsername = "testusername";
            string password = "testpassword";
            string wrongPassword = "wrongpassword";

            // Act
            var ex = Assert.Throws<Exception>(() => accountService.Login(expectedUsername, wrongPassword));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("Unauthorized"));//Репозиторій повертає "Unauthorized"

        }


        //Тест на реєстрацію юзера
        [Test]
        public void Successful_Registration()
        {
            // Arrange
            string expectedUsername = "newuser";
            string password = "password";
            string firstName = "New";
            string lastName = "User";
            // Act
            accountService.Register(expectedUsername, firstName, lastName, password);
            var registeredAccount = accountRepository.GetByUserName(expectedUsername);
            // Assert
            Assert.That(registeredAccount, Is.Not.Null, "Registered account should not be null");
            Assert.That(registeredAccount.UserName, Is.EqualTo(expectedUsername), "Usernames should match");
            Assert.That(registeredAccount.FirstName, Is.EqualTo(firstName), "First names should match");
            Assert.That(registeredAccount.LastName, Is.EqualTo(lastName), "Last names should match");
            Assert.That(registeredAccount.PasswordHash, Is.Not.Null.Or.Empty, "Password hash should not be null or empty");
        }
    }
}
