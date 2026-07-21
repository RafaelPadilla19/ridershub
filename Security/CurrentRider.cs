using System.IdentityModel.Tokens.Jwt;

namespace RidersHub.Security;

/// <summary>Rider autenticado en la petición actual (leído del JWT).</summary>
public sealed class CurrentRider(IHttpContextAccessor accessor)
{
    public Guid? RiderId
    {
        get
        {
            var sub = accessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}
