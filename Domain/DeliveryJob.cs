namespace RidersHub.Domain;

public enum JobStatus { Open = 0, Accepted = 1, Delivered = 2, Cancelled = 3 }

/// <summary>
/// Un pedido de un restaurante de Comanda publicado al pool porque no tenía rider propio
/// disponible. RidersHub es agnóstico del dominio de Comanda: solo guarda referencias (strings/Guid)
/// para poder avisarle de vuelta al publicar/aceptar/entregar (mismo patrón que Charge en PaymentsHub).
/// </summary>
public class DeliveryJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // De dónde viene (Comanda.Api es quien publica; no hay FK real, solo trazabilidad).
    public string TenantId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;      // "#1042" (visible al rider)
    public string RestaurantName { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public double? PickupLat { get; set; }
    public double? PickupLng { get; set; }
    public string DropoffAddress { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;           // para filtrar riders cercanos
    public decimal DeliveryFee { get; set; }                   // lo que el restaurante le paga al rider (fuera de Comanda)
    public string Notes { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;  // para que el rider contacte al cliente

    public JobStatus Status { get; set; } = JobStatus.Open;
    public Guid? RiderId { get; set; }
    public string RiderName { get; set; } = string.Empty;      // snapshot para no requerir join

    // Webhook de vuelta a Comanda.Api cuando cambia de estado (mismo patrón que Charge.CallbackUrl/Key).
    public string CallbackUrl { get; set; } = string.Empty;
    public string CallbackKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
