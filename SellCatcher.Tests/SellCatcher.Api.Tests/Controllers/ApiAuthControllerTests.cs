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
            accountRepository = new AccountRepository();
            // Створюємо налаштування для JWT(Частина написана клудом фактично створюємо фейк токен)
            var authSettings = new AuthSettings
            {
                SecretKey = "ThisIsAVerySecretKeyForTestingPurposesOnly12345", // мінімум 32 символи
                TokenLifetime = TimeSpan.FromHours(1)
            };

            var options = Options.Create(authSettings);
            jwtService = new JWTService(options);

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


        /***
        [Test]
        public void Registration_WithTheSameUsername_ThrowsError()
        {
            //arrange
            string expectedUsername = "user";
            string password = "password";
            
            
            string sameUsername = "user";
            string samePassword = "password";
            string firstName = "New";
            string lastName = "User";

            Account testAccount = new Account
            {
                UserName = expectedUsername,
                FirstName = "Test",
                LastName = "User"
            };

            var passwordHasher = new PasswordHasher<Account>();
            testAccount.PasswordHash = passwordHasher.HashPassword(testAccount, password);

            accountRepository.Add(testAccount);
            //act
            
            var ex = Assert.Throws<Exception>(() => accountService.Register(sameUsername, firstName, lastName, samePassword));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("Username already exists"));
        }
        ***/
    }
}
