namespace RidersHub.Domain;

/// <summary>Catálogo de vehículos disponibles para el registro/perfil del rider.</summary>
public class VehicleType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
