using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RidersHub.Domain;
using RidersHub.Persistence;

namespace RidersHub.Features;

/// <summary>El restaurante califica al rider tras la entrega (llamada entrante desde Comanda.Api, con API key).</summary>
public sealed class RateJobRequest
{
    public Guid JobId { get; set; }
    public int Stars { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public sealed class RateJobValidator : Validator<RateJobRequest>
{
    public RateJobValidator() => RuleFor(x => x.Stars).InclusiveBetween(1, 5).WithMessage("La calificación debe ser de 1 a 5.");
}

public sealed class RateJobEndpoint(RidersDbContext db) : Endpoint<RateJobRequest>
{
    public override void Configure() { Post("/internal/jobs/{jobId}/rate"); AllowAnonymous(); }

    public override async Task HandleAsync(RateJobRequest req, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([req.JobId], ct);
        if (job is null || job.RiderId is not { } riderId || job.Status != JobStatus.Delivered)
        {
            await HttpContext.Response.WriteAsJsonAsync(
                new { codigo = "ratings.job_invalido", mensaje = "Este pedido no se puede calificar." }, ct);
            HttpContext.Response.StatusCode = 400;
            return;
        }
        if (await db.Ratings.AnyAsync(r => r.DeliveryJobId == req.JobId, ct))
        {
            HttpContext.Response.StatusCode = 409;
            await HttpContext.Response.WriteAsJsonAsync(new { codigo = "ratings.ya_calificado", mensaje = "Este pedido ya fue calificado." }, ct);
            return;
        }

        db.Ratings.Add(new RiderRating { RiderId = riderId, DeliveryJobId = req.JobId, TenantId = job.TenantId, Stars = req.Stars, Comment = req.Comment.Trim() });

        var rider = await db.Riders.FindAsync([riderId], ct);
        if (rider is not null)
        {
            var total = rider.AverageRating * rider.RatingCount + req.Stars;
            rider.RatingCount += 1;
            rider.AverageRating = total / rider.RatingCount;
        }

        await db.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
