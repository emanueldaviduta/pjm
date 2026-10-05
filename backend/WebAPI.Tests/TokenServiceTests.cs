using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using WebAPI.Models;
using WebAPI.Services;

namespace WebAPI.Tests;

/// <summary>
/// Example 1: a plain unit test ([Fact]) for a class with no database.
/// Pattern: Arrange the inputs, Act once, Assert on the result.
/// </summary>
public class TokenServiceTests
{
    [Fact]
    public void GenerateToken_IncludesUserIdAndEmailClaims()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TokenKey"] = TestData.TokenKey })
            .Build();
        var service = new TokenService(config);
        var user = TestData.User(id: 42, email: "ana@example.com");

        // Act
        var token = service.GenerateToken(user);

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c => c.Type == "nameid" && c.Value == "42");
        Assert.Contains(jwt.Claims, c => c.Type == "unique_name" && c.Value == "ana@example.com");
        Assert.True(jwt.ValidTo > DateTime.UtcNow, "The token should not be expired when issued.");
    }
}

/// <summary>Shared values so each test only spells out what it cares about.</summary>
internal static class TestData
{
    // HMAC-SHA512 needs a key of at least 64 bytes.
    public const string TokenKey = "test-token-key-that-is-long-enough-for-hmac-sha512-signing-0123456789";

    public static AppUser User(int id = 1, string email = "user@example.com") => new()
    {
        Id = id,
        FirstName = "Ana",
        LastName = "Pop",
        Email = email,
        PasswordHash = [],
        PasswordSalt = []
    };
}
