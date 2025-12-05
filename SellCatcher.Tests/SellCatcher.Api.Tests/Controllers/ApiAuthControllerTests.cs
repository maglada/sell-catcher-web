using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Moq;
using SellCatcher.Api.Controllers;
using SellCatcher.Api.DTOs;
using SellCatcher.Api.DTOs.Token;
using SellCatcher.Api.DTOs.User;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;

namespace SellCatcher.Tests.SellCatcher.Api.Tests.Controllers
{
    [TestFixture]
    public class ApiAuthControllerTests
    {

        private Mock<ITokenRepository> _mockTokenRepo;
        private AccountRepository _accountRepository;
        private AccountService _accountService;
        private JWTService _jwtService;
        private RefreshTokenService _refreshTokenService;
        private AuthController _authController;
        private AuthSettings _authSettings;
        private Account _testAccount;

        private const string TestDbPath = "test_auth.db";
        private const string ValidUsername = "testuser";
        private const string ValidPassword = "Test@123";
        private const string ValidFirstName = "John";
        private const string ValidLastName = "Doe";
        private const string ValidEmail = "john@test.com";
        private const string InvalidPassword = "wrongpassword";
        private const string NonExistentUser = "ghost_user";



        [SetUp]
        public void Setup()
        {
            // Налаштування
            _authSettings = new AuthSettings
            {
                SecretKey = "super-secret-key-for-testing-must-be-at-least-32-chars",
                TokenLifetime = TimeSpan.FromMinutes(15),
                RefreshTokenLifetime = TimeSpan.FromDays(10)
            };
            var options = Options.Create(_authSettings);

            // Mock для ITokenRepository
            _mockTokenRepo = new Mock<ITokenRepository>();
            SetupTokenRepositoryMock();


            _accountRepository = new AccountRepository(TestDbPath);
            _accountService = new AccountService(_accountRepository);
            _jwtService = new JWTService(options);
            _refreshTokenService = new RefreshTokenService(_mockTokenRepo.Object, _jwtService, options);


            _authController = new AuthController(
                _accountService,
                _refreshTokenService,
                _mockTokenRepo.Object,
                _accountRepository,
                _jwtService
            );

            // Створюємо тестового юзера
            CreateTestAccount();
        }

        [TearDown]
        public void TearDown()
        {
            _accountRepository?.Dispose();

            if (File.Exists(TestDbPath))
                File.Delete(TestDbPath);
        }



        private void SetupTokenRepositoryMock()
        {
            // Зберігаємо токени в пам'яті для тестів
            var storedTokens = new Dictionary<string, RefreshToken>();
            var blacklist = new HashSet<string>();

            _mockTokenRepo
                .Setup(x => x.SaveRefreshTokenAsync(It.IsAny<RefreshToken>()))
                .Callback<RefreshToken>(token => storedTokens[token.Token] = token)
                .Returns(Task.CompletedTask);

            _mockTokenRepo
                .Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync((string token) =>
                    storedTokens.TryGetValue(token, out var t) ? t : null);

            _mockTokenRepo
                .Setup(x => x.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string?>()))
                .Callback<string, string?>((token, replacedBy) =>
                {
                    if (storedTokens.TryGetValue(token, out var t))
                    {
                        t.Revoked = true;
                        t.ReplacedBy = replacedBy;
                    }
                })
                .Returns(Task.CompletedTask);

            _mockTokenRepo
                .Setup(x => x.AddToBlacklistAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>()))
                .Callback<string, DateTime, string?>((id, _, _) => blacklist.Add(id))
                .Returns(Task.CompletedTask);

            _mockTokenRepo
                .Setup(x => x.IsBlacklistedAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => blacklist.Contains(id));
        }

        private void CreateTestAccount()
        {
            _testAccount = new Account
            {
                UserName = ValidUsername,
                FirstName = ValidFirstName,
                LastName = ValidLastName,
                PasswordHash = string.Empty
            };

            var hasher = new PasswordHasher<Account>();
            _testAccount.PasswordHash = hasher.HashPassword(_testAccount, ValidPassword);
            _accountRepository.Add(_testAccount);
        }



        [Test]
        public void Register_ValidData_ReturnsOk()
        {
            // Arrange
            var request = new RegisterRequestDto
            {
                UserName = "newuser",
                FirstName = "New",
                LastName = "User",
                Password = "NewUser@123"
            };
            // Act
            var result = _authController.Register(request) as OkObjectResult;
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Value.ToString().Contains("Registration successful"), Is.True);
        }
        [Test]
        public async Task Login_ValidCredentials_ReturnsTokens()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                UserName = ValidUsername,
                Password = ValidPassword
            };
            // Act
            var result = await _authController.Login(request) as OkObjectResult;
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            var tokens = result.Value as TokenResponseDto;
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.AccessToken, Is.Not.Null.And.Not.Empty);
            Assert.That(tokens.RefreshToken, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                UserName = ValidUsername,
                Password = InvalidPassword
            };
            // Act
            var result = await _authController.Login(request) as UnauthorizedResult;
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(401));

        }
        [Test]
        public async Task LoginWithNonExistingUser_ReturnsUnauthorized()
        {
            var request = new LoginRequestDto
            {
                UserName = NonExistentUser,
                Password = ValidPassword
            };

            var result = await _authController.Login(request) as UnauthorizedResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task Login_SavesRefreshTokenToRepository()
        {
            var request = new LoginRequestDto
            {
                UserName = ValidUsername,
                Password = ValidPassword
            };

            await _authController.Login(request);

            _mockTokenRepo.Verify(
                x => x.SaveRefreshTokenAsync(It.Is<RefreshToken>(t => t.AccountId == _testAccount.Id)),
                Times.Once);

        }
        [Test]
        public async Task Refresh_ValidRefreshToken_ReturnsNewTokens()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                UserName = ValidUsername,
                Password = ValidPassword
            };
            var loginResult = await _authController.Login(loginRequest) as OkObjectResult;
            var tokens = loginResult.Value as TokenResponseDto;
            var refreshRequest = new RefreshRequestDto
            {
                RefreshToken = tokens.RefreshToken
            };
            // Act
            var refreshResult = await _authController.Refresh(refreshRequest) as OkObjectResult;
            // Assert
            Assert.That(refreshResult, Is.Not.Null);
            Assert.That(refreshResult.StatusCode, Is.EqualTo(200));
            var newTokens = refreshResult.Value as TokenResponseDto;
            Assert.That(newTokens, Is.Not.Null);
            Assert.That(newTokens.AccessToken, Is.Not.Null.And.Not.Empty);
            Assert.That(newTokens.RefreshToken, Is.Not.Null.And.Not.Empty);
            Assert.That(newTokens.RefreshToken, Is.Not.EqualTo(tokens.RefreshToken));
        }
        [Test]
        public async Task Refresh_InvalidRefreshToken_ReturnsUnauthorized()
        {
            var result = await _authController.Refresh(new RefreshRequestDto
            {
                RefreshToken = "fake_token_12345"
            });

            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        }
        [Test]
        public async Task Logout_validToken()
        {
            var loginResult = await _authController.Login(new LoginRequestDto
            {
                UserName = ValidUsername,
                Password = ValidPassword
            }) as OkObjectResult;
            var tokens = loginResult?.Value as TokenResponseDto;
            var result = await _authController.Logout(new LogoutRequestDto
            {
                RefreshToken = tokens.RefreshToken
            }) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public void GenerateTokens_ReturnsValidTokenPair()
        {
            var tokens = _jwtService.GenerateTokens(_testAccount);

            Assert.That(tokens.AccessToken, Is.Not.Null.And.Not.Empty);
            Assert.That(tokens.RefreshToken, Is.Not.Null.And.Not.Empty);
            Assert.That(tokens.Jti, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void GetJtiFromAccessToken_ReturnsCorrectJti()
        {
            var tokens = _jwtService.GenerateTokens(_testAccount);
            var extractedJti = _jwtService.GetJtiFromAccessToken(tokens.AccessToken);

            Assert.That(extractedJti, Is.EqualTo(tokens.Jti));
        }



    }
}
    