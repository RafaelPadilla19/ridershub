using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RidersHub.Domain;

namespace RidersHub.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opt = options.Value;

    public (string Token, DateTime ExpiresAtUtc) CreateToken(Rider rider)
    {
        var expires = DateTime.UtcNow.AddMinutes(_opt.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, rider.Id.ToString()),
            new(ClaimTypes.Name, rider.Name),
            new("phone", rider.Phone),
        };

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer, audience: _opt.Audience, claims: claims,
            notBefore: DateTime.UtcNow, expires: expires, signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
