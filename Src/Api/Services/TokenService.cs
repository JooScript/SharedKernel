using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SharedKernel.Api.Services;

/// <summary>
/// Provides JWT access token and refresh token generation services.
/// </summary>
/// <remarks>
/// This service is responsible for creating signed JWT tokens
/// using configuration-defined settings such as issuer, audience,
/// and secret key, as well as generating secure refresh tokens.
/// </remarks>
public class TokenService(IOptions<JwtSettings> options) : IToken
{
    private readonly JwtSettings _settings = options.Value;

    /// <summary>
    /// Generates a signed JWT access token containing the specified claims.
    /// </summary>
    /// <param name="claims">
    /// The claims to include in the JWT token.
    /// </param>
    /// <returns>
    /// A serialized JWT access token.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the JWT secret key is missing in configuration.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the JWT secret key is empty or whitespace.
    /// </exception>
    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        string _secretKey = _settings.Key
            ?? throw new ArgumentNullException("Jwt:Key", "JWT secret key is missing in configuration.");

        if (string.IsNullOrWhiteSpace(_secretKey))
            throw new ArgumentException("JWT secret key cannot be empty or whitespace.", nameof(_secretKey));

        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    /// <returns>
    /// A base64-encoded secure random refresh token.
    /// </returns>
    public string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];

        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }

        return Convert.ToBase64String(randomNumber);
    }
}