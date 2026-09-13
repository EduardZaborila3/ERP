using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpServer.Models;
using Microsoft.IdentityModel.Tokens;

namespace ErpServer.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user, string? roleName = null)
    {
        var secret = _config["JwtSettings:Secret"] ?? "SuperSecretEnterpriseErpJwtSigningKey2026!DefaultKey";
        var issuer = _config["JwtSettings:Issuer"] ?? "ErpServer";
        var audience = _config["JwtSettings:Audience"] ?? "ErpClient";
        var expiryDays = int.TryParse(_config["JwtSettings:ExpiryDays"], out var days) ? days : 7;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, roleName ?? (user.AppRoleId == 1 ? "Admin" : "User")),
            new("appRoleId", user.AppRoleId.ToString()),
            new("jobTitleId", user.JobTitleId.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(expiryDays),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
