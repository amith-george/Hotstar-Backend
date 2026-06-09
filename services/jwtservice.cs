using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotstarApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace HotstarApi.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey   = jwtSettings["SecretKey"]
                          ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        var issuer      = jwtSettings["Issuer"]   ?? "HotstarApi";
        var audience    = jwtSettings["Audience"] ?? "HotstarClients";
        var expiryDays  = int.TryParse(jwtSettings["ExpiryDays"], out var d) ? d : 7;

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Role,               user.Role),
            // Add subscription tier as a custom claim so controllers can gate content easily
            new("subscriptionId", user.SubscriptionId?.ToString() ?? "1"),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
