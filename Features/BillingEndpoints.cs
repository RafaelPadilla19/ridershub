using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RidersHub.Domain;
using RidersHub.Persistence;
using RidersHub.Security;
using RidersHub.Services;

namespace RidersHub.Features;

// ---------------- Suscripción Pro del rider ----------------

public sealed class RiderBillingDto
{
    public string Plan { get; set; } = string.Empty;
    public bool HasActivePro { get; set; }
    public DateTime? SubscriptionEndsAt { get; set; }
    public decimal ProPriceMonthly { get; set; }
    public List<RiderPaymentDto> Payments { get; set; } = new();
}

public sealed class RiderPaymentDto
{
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? PeriodEndsAt { get; set; }
}

public sealed class GetRiderBillingEndpoint(RidersDbContext db, CurrentRider current, IConfiguration config)
    : EndpointWithoutRequest<RiderBillingDto>
{
    public override void Configure() => Get("/riders/billing");

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (current.RiderId is not { } id || await db.Riders.FindAsync([id], ct) is not { } rider)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var history = await db.SubscriptionPayments.Where(p => p.RiderId == id)
            .OrderByDescending(p => p.CreatedAt).Take(10)
            .Select(p => new RiderPaymentDto { Amount = p.Amount, IsPaid = p.IsPaid, CreatedAt = p.CreatedAt, PaidAt = p.PaidAt, PeriodEndsAt = p.PeriodEndsAt })
            .ToListAsync(ct);

        await Send.OkAsync(new RiderBillingDto
        {
            Plan = rider.Plan.ToString(), HasActivePro = rider.HasActivePro, SubscriptionEndsAt = rider.SubscriptionEndsAt,
            ProPriceMonthly = decimal.Parse(config["Billing:ProPriceMonthly"] ?? "5.00"),
            Payments = history,
        }, ct);
    }
}

public sealed class RiderCheckoutRequest { public string ReturnUrl { get; set; } = string.Empty; }

public sealed class RiderCheckoutValidator : Validator<RiderCheckoutRequest>
{
    public RiderCheckoutValidator() => RuleFor(x => x.ReturnUrl).NotEmpty().WithMessage("Falta la URL de retorno.");
}

public sealed class RiderCheckoutResponse { public string PaymentUrl { get; set; } = string.Empty; }

/// <summary>Inicia el cobro de la suscripción Pro del rider (mismo motor/PaymentsHub que usan los restaurantes).</summary>
public sealed class RiderCheckoutEndpoint(
    RidersDbContext db, CurrentRider current, PaymentsHubClient paymentsClient, IConfiguration config)
    : Endpoint<RiderCheckoutRequest, RiderCheckoutResponse>
{
    public override void Configure() => Post("/riders/billing/checkout");

    public override async Task HandleAsync(RiderCheckoutRequest req, CancellationToken ct)
    {
        if (current.RiderId is not { } id || await db.Riders.FindAsync([id], ct) is not { } rider)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var amount = decimal.Parse(config["Billing:ProPriceMonthly"] ?? "5.00");

        // Idempotencia: reutiliza un cobro pendiente si ya existe (mismo fix que en Comanda).
        var pending = await db.SubscriptionPayments
            .Where(p => p.RiderId == id && !p.IsPaid)
            .OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync(ct);

        RiderSubscriptionPayment sub;
        if (pending is not null) { sub = pending; sub.Amount = amount; }
        else
        {
            sub = new RiderSubscriptionPayment { RiderId = id, Amount = amount, PeriodMonths = 1 };
            db.SubscriptionPayments.Add(sub);
        }
        await db.SaveChangesAsync(ct);

        var charge = await paymentsClient.CreateSubscriptionChargeAsync(
            amount, $"Comanda Riders Pro · {rider.Name}", sub.Id.ToString(), id.ToString(), req.ReturnUrl, ct);
        if (charge is null)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new { codigo = "riders.cobro", mensaje = "No se pudo iniciar el cobro. Intenta de nuevo." }, ct);
            return;
        }

        sub.PaymentRef = charge.ChargeId.ToString();
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new RiderCheckoutResponse { PaymentUrl = charge.PayUrl }, ct);
    }
}

// ---------------- Callback de PaymentsHub (confirma el pago) ----------------

public sealed class PaymentCallbackRequest
{
    public Guid ChargeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? OrderRef { get; set; }
}

public sealed class PaymentCallbackEndpoint(RidersDbContext db, IConfiguration config) : Endpoint<PaymentCallbackRequest>
{
    public override void Configure() { Post("/internal/payments/callback"); AllowAnonymous(); }

    public override async Task HandleAsync(PaymentCallbackRequest req, CancellationToken ct)
    {
        var key = HttpContext.Request.Headers["X-Callback-Key"].FirstOrDefault();
        var expected = config["Payments:CallbackSecret"];
        if (string.IsNullOrWhiteSpace(expected) || key != expected)
        {
            await Send.ResponseAsync(new { ok = false }, 401, ct);
            return;
        }

        var paid = req.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase);
        if (paid && Guid.TryParse(req.OrderRef, out var subId))
        {
            var sub = await db.SubscriptionPayments.FindAsync([subId], ct);
            if (sub is not null && !sub.IsPaid)
            {
                sub.IsPaid = true;
                sub.PaidAt = DateTime.UtcNow;

                var rider = await db.Riders.FindAsync([sub.RiderId], ct);
                if (rider is not null)
                {
                    var baseDate = rider.SubscriptionEndsAt is { } end && end > DateTime.UtcNow ? end : DateTime.UtcNow;
                    rider.SubscriptionEndsAt = baseDate.AddMonths(sub.PeriodMonths);
                    rider.Plan = RiderPlan.Pro;
                    sub.PeriodEndsAt = rider.SubscriptionEndsAt;
                }
                await db.SaveChangesAsync(ct);
            }
        }

        await Send.OkAsync(new { ok = true }, ct);
    }
}
