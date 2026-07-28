using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using RidersHub.Domain;
using RidersHub.Persistence;

namespace RidersHub.Features;

public sealed class VehicleTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Catálogo de vehículos para el formulario de registro/edición de perfil.</summary>
public sealed class ListVehicleTypesEndpoint(RidersDbContext db) : EndpointWithoutRequest<List<VehicleTypeDto>>
{
    public override void Configure() { Get("/vehicle-types"); AllowAnonymous(); }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var list = await db.VehicleTypes.Where(v => v.IsActive)
            .OrderBy(v => v.SortOrder)
            .Select(v => new VehicleTypeDto { Id = v.Id, Name = v.Name })
            .ToListAsync(ct);
        await Send.OkAsync(list, ct);
    }
}
