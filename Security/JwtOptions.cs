namespace RidersHub.Security;

public sealed class JwtOptions
{
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "RidersHub";
    public string Audience { get; set; } = "RidersHubClients";
    public int ExpiryMinutes { get; set; } = 480;
}
