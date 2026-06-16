using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace SharedKernel.Bl.Contracts.Auth;

public interface IToken
{
    public string GenerateAccessToken(IEnumerable<Claim> claims);

    public string GenerateRefreshToken();
}