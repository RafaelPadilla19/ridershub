using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RidersHub.Domain;
using RidersHub.Persistence;
using RidersHub.Security;
using RidersHub.Services;

namespace RidersHub.Features;

/// <summary>Minutos que un job queda visible SOLO para riders Pro antes de abrirse a todos (Free incluido).</summary>
public static class VisibilityPolicy
{
    public const int ProExclusiveMinutes = 3;
}

public sealed class JobDto
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string DropoffAddress { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiderName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static JobDto From(DeliveryJob j) => new()
    {
        Id = j.Id, OrderCode = j.OrderCode, RestaurantName = j.RestaurantName, PickupAddress = j.PickupAddress,
        DropoffAddress = j.DropoffAddress, Zone = j.Zone, DeliveryFee = j.DeliveryFee, Notes = j.Notes,
        Status = j.Status.ToString(), RiderName = j.RiderName, CreatedAt = j.CreatedAt,
    };
}

// ---------------- Publicar (llamada entrante desde Comanda.Api, protegida por API key) ----------------

public sealed class PublishJobRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string DropoffAddress { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string CallbackKey { get; set; } = string.Empty;
}

public sealed class PublishJobValidator : Validator<PublishJobRequest>
{
    public PublishJobValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Zone).NotEmpty().WithMessage("La zona es obligatoria para publicar el pedido al pool.");
    }
}

public sealed class PublishJobEndpoint(RidersDbContext db) : Endpoint<PublishJobRequest, JobDto>
{
    public override void Configure() { Post("/internal/jobs"); AllowAnonymous(); } // lo protege ApiKeyMiddleware, no JWT

    public override async Task HandleAsync(PublishJobRequest req, CancellationToken ct)
    {
        var job = new DeliveryJob
        {
            TenantId = req.TenantId, OrderId = req.OrderId, OrderCode = req.OrderCode, RestaurantName = req.RestaurantName,
            PickupAddress = req.PickupAddress, DropoffAddress = req.DropoffAddress, Zone = req.Zone.Trim(),
            DeliveryFee = req.DeliveryFee, Notes = req.Notes, CallbackUrl = req.CallbackUrl, CallbackKey = req.CallbackKey,
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);
        await Send.OkAsync(JobDto.From(job), ct);
    }
}

// ---------------- Listar pedidos disponibles (rider autenticado) ----------------

public sealed class ListJobsEndpoint(RidersDbContext db, CurrentRider current) : EndpointWithoutRequest<List<JobDto>>
{
    public override void Configure() => Get("/jobs");

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (current.RiderId is not { } id || await db.Riders.FindAsync([id], ct) is not { } rider)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var open = await db.Jobs.Where(j => j.Status == JobStatus.Open && j.Zone == rider.Zone)
            .OrderBy(j => j.CreatedAt).ToListAsync(ct);

        // Free ve solo los jobs que ya llevan el retraso exclusivo-Pro; Pro los ve todos al instante.
        var visible = rider.HasActivePro
            ? open
            : open.Where(j => j.CreatedAt <= DateTime.UtcNow.AddMinutes(-VisibilityPolicy.ProExclusiveMinutes)).ToList();

        await Send.OkAsync(visible.Select(JobDto.From).ToList(), ct);
    }
}

// ---------------- Aceptar ----------------

public sealed class AcceptJobRequest { public Guid Id { get; set; } }

public sealed class AcceptJobEndpoint(RidersDbContext db, CurrentRider current, JobCallbackNotifier notifier)
    : Endpoint<AcceptJobRequest, JobDto>
{
    public override void Configure() => Post("/jobs/{id}/accept");

    public override async Task HandleAsync(AcceptJobRequest req, CancellationToken ct)
    {
        if (current.RiderId is not { } riderId || await db.Riders.FindAsync([riderId], ct) is not { } rider)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var job = await db.Jobs.FindAsync([req.Id], ct);
        if (job is null) { await Send.NotFoundAsync(ct); return; }
        if (job.Status != JobStatus.Open)
        {
            HttpContext.Response.StatusCode = 409;
            await HttpContext.Response.WriteAsJsonAsync(new { codigo = "jobs.no_disponible", mensaje = "Este pedido ya fue tomado por otro rider." }, ct);
            return;
        }

        job.Status = JobStatus.Accepted;
        job.RiderId = riderId;
        job.RiderName = rider.Name;
        job.AcceptedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(job, ct);

        await Send.OkAsync(JobDto.From(job), ct);
    }
}

// ---------------- Entregar ----------------

public sealed class DeliverJobRequest { public Guid Id { get; set; } }

public sealed class DeliverJobEndpoint(RidersDbContext db, CurrentRider current, JobCallbackNotifier notifier)
    : Endpoint<DeliverJobRequest, JobDto>
{
    public override void Configure() => Post("/jobs/{id}/deliver");

    public override async Task HandleAsync(DeliverJobRequest req, CancellationToken ct)
    {
        if (current.RiderId is not { } riderId) { await Send.NotFoundAsync(ct); return; }
        var job = await db.Jobs.FindAsync([req.Id], ct);
        if (job is null || job.RiderId != riderId)
        {
            HttpContext.Response.StatusCode = 403;
            await HttpContext.Response.WriteAsJsonAsync(new { codigo = "jobs.no_autorizado", mensaje = "Este pedido no está asignado a ti." }, ct);
            return;
        }
        if (job.Status != JobStatus.Accepted)
        {
            HttpContext.Response.StatusCode = 409;
            await HttpContext.Response.WriteAsJsonAsync(new { codigo = "jobs.estado_invalido", mensaje = "Este pedido no está en camino." }, ct);
            return;
        }

        job.Status = JobStatus.Delivered;
        job.DeliveredAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(job, ct);

        await Send.OkAsync(JobDto.From(job), ct);
    }
}

/// <summary>Historial de entregas del rider autenticado (para su pantalla "Mis viajes").</summary>
public sealed class MyJobsEndpoint(RidersDbContext db, CurrentRider current) : EndpointWithoutRequest<List<JobDto>>
{
    public override void Configure() => Get("/jobs/mine");

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (current.RiderId is not { } riderId) { await Send.NotFoundAsync(ct); return; }
        var mine = await db.Jobs.Where(j => j.RiderId == riderId)
            .OrderByDescending(j => j.CreatedAt).Take(50).ToListAsync(ct);
        await Send.OkAsync(mine.Select(JobDto.From).ToList(), ct);
    }
}
