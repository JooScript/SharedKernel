using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using SharedKernel.Api.Services;
using SharedKernel.Bl.Models;

namespace SharedKernel.Api.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_ReturnsSignedTokenWithClaimsAndSettings()
    {
        var settings = CreateSettings();
        var sut = new TokenService(Options.Create(settings));
        var userId = Guid.NewGuid();
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "user@example.com"),
            new(ClaimTypes.Role, "User")
        ];

        var token = sut.GenerateAccessToken(claims);

        Assert.False(string.IsNullOrWhiteSpace(token));

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(settings.Issuer, parsed.Issuer);
        Assert.Contains(settings.Audience, parsed.Audiences);
        Assert.Contains(parsed.Claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == userId.ToString());
        Assert.Contains(parsed.Claims, claim => claim.Type == ClaimTypes.Email && claim.Value == "user@example.com");
        Assert.Contains(parsed.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "User");
        Assert.InRange(parsed.ValidTo, DateTime.UtcNow.AddMinutes(4), DateTime.UtcNow.AddMinutes(6));
    }

    [Fact]
    public void GenerateAccessToken_WithNoClaims_ReturnsValidToken()
    {
        var sut = new TokenService(Options.Create(CreateSettings()));

        var token = sut.GenerateAccessToken([]);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.DoesNotContain(parsed.Claims, claim => claim.Type == ClaimTypes.NameIdentifier);
        Assert.DoesNotContain(parsed.Claims, claim => claim.Type == ClaimTypes.Email);
        Assert.DoesNotContain(parsed.Claims, claim => claim.Type == ClaimTypes.Role);
    }

    [Fact]
    public void GenerateAccessToken_WithNullKey_ThrowsArgumentNullException()
    {
        var settings = CreateSettings();
        settings.Key = null!;
        var sut = new TokenService(Options.Create(settings));

        var exception = Assert.Throws<ArgumentNullException>(
            () => sut.GenerateAccessToken([]));

        Assert.Equal("Jwt:Key", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateAccessToken_WithEmptyKey_ThrowsArgumentException(string key)
    {
        var settings = CreateSettings();
        settings.Key = key;
        var sut = new TokenService(Options.Create(settings));

        var exception = Assert.Throws<ArgumentException>(
            () => sut.GenerateAccessToken([]));

        Assert.Equal("_secretKey", exception.ParamName);
    }

    private static JwtSettings CreateSettings() => new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = "this-is-a-test-signing-key-that-is-long-enough-for-hmac-sha256",
        ExpireMinutes = 5
    };
}
