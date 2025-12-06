using Microsoft.Extensions.Options;
using Moq;
using SellCatcher.Api.Controllers;
using SellCatcher.Api.DTOs.Token;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SellCatcher.Tests.Services.JwtServiceTests
{
    
        [TestFixture]
        public class JWTServiceTests
        {
            private JWTService _jwtService;
            private AuthSettings _authSettings;
            private Account _testAccount;
            private TokenResponseDto _validTokenPair;
            private const string InvalidToken = "invalid-token-string";
            private const string EmptyString = "";

            [SetUp]
            public void Setup()
            {
                _authSettings = new AuthSettings
                {
                    SecretKey = "super-secret-key-for-testing-must-be-at-least-32-chars-long",
                    TokenLifetime = TimeSpan.FromMinutes(15),
                    RefreshTokenLifetime = TimeSpan.FromDays(10)
                };
                var options = Options.Create(_authSettings);
                _jwtService = new JWTService(options);

                _testAccount = new Account
                {
                    Id = Guid.NewGuid(),
                    UserName = "testuser",
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john@test.com"
                };

                // Generate a valid token pair for reuse in tests
                _validTokenPair = _jwtService.GenerateTokens(_testAccount);
            }

            [Test]
            public void GenerateTokens_ValidAccount_ReturnsValidTokenPair()
            {
                // Act
                var result = _jwtService.GenerateTokens(_testAccount);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.AccessToken, Is.Not.Null.And.Not.Empty);
                Assert.That(result.RefreshToken, Is.Not.Null.And.Not.Empty);
                Assert.That(result.Jti, Is.Not.Null.And.Not.Empty);
                Assert.That(result.AccessTokenExpiresAt, Is.GreaterThan(DateTime.UtcNow));
            }

            [Test]
            public void GenerateTokens_NullAccount_ThrowsArgumentNullException()
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() => _jwtService.GenerateTokens(null));
            }

            [Test]
            public void GenerateTokens_AccessTokenExpiresCorrectly()
            {
                // Act
                var result = _jwtService.GenerateTokens(_testAccount);
                var expectedExpiry = DateTime.UtcNow.Add(_authSettings.TokenLifetime);

                // Assert 
                Assert.That(result.AccessTokenExpiresAt, Is.EqualTo(expectedExpiry).Within(TimeSpan.FromSeconds(5)));
            }

            [Test]
            public void GenerateTokens_ContainsCorrectClaims()
            {
                // Act
                var result = _jwtService.GenerateTokens(_testAccount);
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(result.AccessToken);

                // Assert
                Assert.That(token.Claims.Any(c => c.Type == "username" && c.Value == _testAccount.UserName));
                Assert.That(token.Claims.Any(c => c.Type == "firstName" && c.Value == _testAccount.FirstName));
                Assert.That(token.Claims.Any(c => c.Type == "lastName" && c.Value == _testAccount.LastName));
                Assert.That(token.Claims.Any(c => c.Type == "id" && c.Value == _testAccount.Id.ToString()));
                Assert.That(token.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Jti));
            }

            [Test]
            public void GetJtiFromAccessToken_ValidToken_ReturnsCorrectJti()
            {
                // Arrange
                var tokenPair = _jwtService.GenerateTokens(_testAccount);

                // Act
                var jti = _jwtService.GetJtiFromAccessToken(tokenPair.AccessToken);

                // Assert
                Assert.That(jti, Is.EqualTo(tokenPair.Jti));
            }

            [Test]
            public void GetJtiFromAccessToken_InvalidToken_ReturnsNull()
            {
                // Act
                var jti = _jwtService.GetJtiFromAccessToken("invalid-token");

                // Assert
                Assert.That(jti, Is.Null);
            }

            [Test]
            public void GetJtiFromAccessToken_NullOrEmpty_ReturnsNull()
            {
                // Act & Assert
                Assert.That(_jwtService.GetJtiFromAccessToken(null), Is.Null);
                Assert.That(_jwtService.GetJtiFromAccessToken(""), Is.Null);
            }

            [Test]
            public void GetExpiryFromAccessToken_ValidToken_ReturnsCorrectExpiry()
            {
                // Arrange
                var tokenPair = _jwtService.GenerateTokens(_testAccount);

                // Act
                var expiry = _jwtService.GetExpiryFromAccessToken(tokenPair.AccessToken);

                // Assert
                Assert.That(expiry, Is.Not.Null);
                Assert.That(expiry.Value, Is.EqualTo(tokenPair.AccessTokenExpiresAt).Within(TimeSpan.FromSeconds(5)));
            }

            [Test]
            public void GetExpiryFromAccessToken_InvalidToken_ReturnsNull()
            {
                // Act
                var expiry = _jwtService.GetExpiryFromAccessToken("invalid-token");

                // Assert
                Assert.That(expiry, Is.Null);
            }

            [Test]
            public void GetExpiryFromAccessToken_NullOrEmpty_ReturnsNull()
            {
                // Act & Assert
                Assert.That(_jwtService.GetExpiryFromAccessToken(null), Is.Null);
                Assert.That(_jwtService.GetExpiryFromAccessToken(""), Is.Null);
            }

            [Test]
            public void GenerateTokens_RefreshTokenIsBase64()
            {
                // Act
                var result = _jwtService.GenerateTokens(_testAccount);

                // Assert
                Assert.DoesNotThrow(() => Convert.FromBase64String(result.RefreshToken));
                Assert.That(Convert.FromBase64String(result.RefreshToken).Length, Is.EqualTo(64));
            }

            [Test]
            public void GenerateTokens_MultipleCallsGenerateDifferentTokens()
            {
                // Act
                var result1 = _jwtService.GenerateTokens(_testAccount);
                var result2 = _jwtService.GenerateTokens(_testAccount);

                // Assert
                Assert.That(result1.AccessToken, Is.Not.EqualTo(result2.AccessToken));
                Assert.That(result1.RefreshToken, Is.Not.EqualTo(result2.RefreshToken));
                Assert.That(result1.Jti, Is.Not.EqualTo(result2.Jti));
            }

            [Test]
            public void GenerateTokens_AccountWithoutFirstName_StillWorks()
            {
                // Arrange
                var account = new Account
                {
                    Id = Guid.NewGuid(),
                    UserName = "minimal",
                    FirstName = null,
                    LastName = null
                };

                // Act
                var result = _jwtService.GenerateTokens(account);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.AccessToken, Is.Not.Null.And.Not.Empty);
            }
        }
    }
