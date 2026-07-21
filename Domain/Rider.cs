namespace RidersHub.Domain;

/// <summary>Nivel de suscripción del rider. Free = visibilidad limitada; Pro = acceso completo y prioridad.</summary>
public enum RiderPlan { Free = 0, Pro = 1 }

/// <summary>Rider independiente registrado en el pool (no pertenece a ningún restaurante de Comanda).</summary>
public class Rider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;   // login
    public string PasswordHash { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;    // ciudad/zona donde opera
    public string VehicleType { get; set; } = string.Empty; // moto, bici, carro...
    public RiderPlan Plan { get; set; } = RiderPlan.Free;
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; }                // insignia verificado (perk Pro)
    public DateTime? SubscriptionEndsAt { get; set; }    // null/vencida = trata como Free

    // Reputación (se recalcula al agregar una calificación).
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Suscripción Pro vigente ahora mismo (no solo el flag Plan, que puede haber vencido).</summary>
    public bool HasActivePro => Plan == RiderPlan.Pro && SubscriptionEndsAt is { } end && end > DateTime.UtcNow;
}
