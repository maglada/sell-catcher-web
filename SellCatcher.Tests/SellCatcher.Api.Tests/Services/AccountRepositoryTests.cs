using SellCatcher.Api.Models;
using SellCatcher.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SellCatcher.Tests.Services.AccRepoServiceTests
{
    [TestFixture]
    public class AccountRepositoryTests
    {
        private AccountRepository _repository;
        private Account _testAccount;
        private Account _duplicateAccount;
        private const string TestDbPath = "test_account_repo.db";
        private const string TestUsername = "testuser";
        private const string TestEmail = "test@example.com";
        private const string TestPassword = "hash";
        private const string DuplicateUsername = "duplicate";

        [SetUp]
        public void Setup()
        {
            if (File.Exists(TestDbPath))
                File.Delete(TestDbPath);

            _repository = new AccountRepository(TestDbPath);

            _testAccount = new Account
            {
                UserName = TestUsername,
                Email = TestEmail,
                FirstName = "Test",
                LastName = "User",
                PasswordHash = TestPassword
            };

            _duplicateAccount = new Account
            {
                UserName = DuplicateUsername,
                PasswordHash = TestPassword
            };
        }

        [TearDown]
        public void TearDown()
        {
            _repository?.Dispose();
            if (File.Exists(TestDbPath))
                File.Delete(TestDbPath);
        }

        [Test]
        public void Add_ValidAccount_SavesSuccessfully()
        {
            // Act
            _repository.Add(_testAccount);
            var retrieved = _repository.GetByUserName(TestUsername);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.UserName, Is.EqualTo(TestUsername));
            Assert.That(retrieved.Email, Is.EqualTo(TestEmail));
            Assert.That(retrieved.FirstName, Is.EqualTo("Test"));
            Assert.That(retrieved.LastName, Is.EqualTo("User"));
        }

        [Test]
        public void Add_AccountWithoutId_GeneratesGuid()
        {
            // Act
            _repository.Add(_testAccount);
            var retrieved = _repository.GetByUserName(TestUsername);

            // Assert
            Assert.That(retrieved.Id, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Add_AccountWithPresetId_KeepsId()
        {
            // Arrange
            var presetId = Guid.NewGuid();
            var account = new Account
            {
                Id = presetId,
                UserName = "testuser",
                PasswordHash = "hash"
            };

            // Act
            _repository.Add(account);
            var retrieved = _repository.GetByUserName("testuser");

            // Assert
            Assert.That(retrieved.Id, Is.EqualTo(presetId));
        }

        [Test]
        public void Add_DuplicateUsername_ThrowsException()
        {
            // Arrange
            _repository.Add(_duplicateAccount);
            var duplicate2 = new Account { UserName = DuplicateUsername, PasswordHash = TestPassword };

            // Act & Assert
            Assert.Throws<LiteDB.LiteException>(() => _repository.Add(duplicate2));
        }

        [Test]
        public void GetByUserName_ExistingUser_ReturnsAccount()
        {
            // Arrange
            var account = new Account
            {
                UserName = "existing",
                Email = "existing@test.com",
                PasswordHash = "hash"
            };
            _repository.Add(account);

            // Act
            var result = _repository.GetByUserName("existing");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserName, Is.EqualTo("existing"));
            Assert.That(result.Email, Is.EqualTo("existing@test.com"));
        }

        [Test]
        public void GetByUserName_NonExistentUser_ReturnsNull()
        {
            // Act
            var result = _repository.GetByUserName("nonexistent");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetByUserName_NullUsername_ReturnsNull()
        {
            // Act
            var result = _repository.GetByUserName(null);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetByUserName_EmptyString_ReturnsNull()
        {
            // Act
            var result = _repository.GetByUserName("");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetById_ExistingId_ReturnsAccount()
        {
            // Arrange
            var account = new Account { UserName = "testuser", PasswordHash = "hash" };
            _repository.Add(account);
            var savedAccount = _repository.GetByUserName("testuser");

            // Act
            var result = _repository.GetById(savedAccount.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(savedAccount.Id));
            Assert.That(result.UserName, Is.EqualTo("testuser"));
        }

        [Test]
        public void GetById_NonExistentId_ReturnsNull()
        {
            // Act
            var result = _repository.GetById(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetByEmail_ExistingEmail_ReturnsAccount()
        {
            // Arrange
            var account = new Account
            {
                UserName = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash"
            };
            _repository.Add(account);

            // Act
            var result = _repository.GetByEmail("test@example.com");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Email, Is.EqualTo("test@example.com"));
            Assert.That(result.UserName, Is.EqualTo("testuser"));
        }

        [Test]
        public void GetByEmail_NonExistentEmail_ReturnsNull()
        {
            // Act
            var result = _repository.GetByEmail("nonexistent@example.com");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Update_ExistingAccount_UpdatesSuccessfully()
        {
            // Arrange
            var account = new Account
            {
                UserName = "testuser",
                Email = "old@example.com",
                FirstName = "Old",
                PasswordHash = "hash"
            };
            _repository.Add(account);
            var saved = _repository.GetByUserName("testuser");

            // Act
            saved.Email = "new@example.com";
            saved.FirstName = "New";
            _repository.Update(saved);
            var updated = _repository.GetById(saved.Id);

            // Assert
            Assert.That(updated.Email, Is.EqualTo("new@example.com"));
            Assert.That(updated.FirstName, Is.EqualTo("New"));
        }

        [Test]
        public void Update_PasswordHash_UpdatesSuccessfully()
        {
            // Arrange
            var account = new Account
            {
                UserName = "testuser",
                PasswordHash = "oldhash"
            };
            _repository.Add(account);
            var saved = _repository.GetByUserName("testuser");

            // Act
            saved.PasswordHash = "newhash";
            _repository.Update(saved);
            var updated = _repository.GetById(saved.Id);

            // Assert
            Assert.That(updated.PasswordHash, Is.EqualTo("newhash"));
        }

        [Test]
        public void Delete_ExistingAccount_RemovesFromDatabase()
        {
            // Arrange
            var account = new Account { UserName = "testuser", PasswordHash = "hash" };
            _repository.Add(account);
            var saved = _repository.GetByUserName("testuser");

            // Act
            _repository.Delete(saved.Id);
            var result = _repository.GetById(saved.Id);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Delete_NonExistentId_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _repository.Delete(Guid.NewGuid()));
        }

        [Test]
        public void GetAll_MultipleAccounts_ReturnsAll()
        {
            // Arrange
            _repository.Add(new Account { UserName = "user1", PasswordHash = "hash" });
            _repository.Add(new Account { UserName = "user2", PasswordHash = "hash" });
            _repository.Add(new Account { UserName = "user3", PasswordHash = "hash" });

            // Act
            var all = _repository.GetAll();

            // Assert
            Assert.That(all.Count, Is.EqualTo(3));
            Assert.That(all.Select(a => a.UserName), Contains.Item("user1"));
            Assert.That(all.Select(a => a.UserName), Contains.Item("user2"));
            Assert.That(all.Select(a => a.UserName), Contains.Item("user3"));
        }

        [Test]
        public void GetAll_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var all = _repository.GetAll();

            // Assert
            Assert.That(all, Is.Empty);
        }

        [Test]
        public void GetAll_AfterDelete_ReturnsRemainingAccounts()
        {
            // Arrange
            _repository.Add(new Account { UserName = "user1", PasswordHash = "hash" });
            _repository.Add(new Account { UserName = "user2", PasswordHash = "hash" });
            var toDelete = _repository.GetByUserName("user1");

            // Act
            _repository.Delete(toDelete.Id);
            var all = _repository.GetAll();

            // Assert
            Assert.That(all.Count, Is.EqualTo(1));
            Assert.That(all.First().UserName, Is.EqualTo("user2"));
        }

        [Test]
        public void Dispose_MultipleCalls_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _repository.Dispose();
                _repository.Dispose();
            });
        }
    }
}
