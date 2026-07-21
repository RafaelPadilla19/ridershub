namespace RidersHub.Domain;

/// <summary>Pago de la suscripción Pro del rider (mismo patrón que SubscriptionPayment en Comanda).</summary>
public class RiderSubscriptionPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RiderId { get; set; }
    public decimal Amount { get; set; }
    public int PeriodMonths { get; set; } = 1;

    public bool IsPaid { get; set; }
    public string PaymentRef { get; set; } = string.Empty;   // chargeId en PaymentsHub
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? PeriodEndsAt { get; set; }               // hasta cuándo queda cubierto (desde el día 1, sin retrofit)
}
