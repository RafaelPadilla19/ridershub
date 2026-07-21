namespace RidersHub.Domain;

/// <summary>Calificación que el restaurante le da al rider tras una entrega.</summary>
public class RiderRating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RiderId { get; set; }
    public Guid DeliveryJobId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public int Stars { get; set; }               // 1-5
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
