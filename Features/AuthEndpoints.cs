using BCrypt.Net;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RidersHub.Domain;
using RidersHub.Persistence;
using RidersHub.Security;

namespace RidersHub.Features;

public sealed class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
}

public sealed class RegisterValidator : Validator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre es obligatorio.");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("El teléfono es obligatorio.");
        RuleFor(x => x.Password).MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");
        RuleFor(x => x.Zone).NotEmpty().WithMessage("La zona donde operas es obligatoria.");
    }
}

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public RiderDto Rider { get; set; } = new();
}

public sealed class RiderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool HasActivePro { get; set; }
    public bool IsVerified { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public DateTime? SubscriptionEndsAt { get; set; }

    public static RiderDto From(Rider r) => new()
    {
        Id = r.Id, Name = r.Name, Phone = r.Phone, Zone = r.Zone, VehicleType = r.VehicleType,
        Plan = r.Plan.ToString(), HasActivePro = r.HasActivePro, IsVerified = r.IsVerified,
        AverageRating = Math.Round(r.AverageRating, 1), RatingCount = r.RatingCount,
        SubscriptionEndsAt = r.SubscriptionEndsAt,
    };
}

public sealed class RegisterEndpoint(RidersDbContext db, JwtTokenService jwt) : Endpoint<RegisterRequest, AuthResponse>
{
    public override void Configure() { Post("/riders/register"); AllowAnonymous(); }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var phone = req.Phone.Trim();
        if (await db.Riders.AnyAsync(r => r.Phone == phone, ct))
        {
            HttpContext.Response.StatusCode = 409;
            await HttpContext.Response.WriteAsJsonAsync(new { codigo = "riders.telefono_en_uso", mensaje = "Ya existe un rider con ese teléfono." }, ct);
            return;
        }

        var rider = new Rider
        {
            Name = req.Name.Trim(), Phone = phone, PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Zone = req.Zone.Trim(), VehicleType = req.VehicleType.Trim(),
        };
        db.Riders.Add(rider);
        await db.SaveChangesAsync(ct);

        var (token, _) = jwt.CreateToken(rider);
        await Send.OkAsync(new AuthResponse { Token = token, Rider = RiderDto.From(rider) }, ct);
    }
}

public sealed class LoginRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginValidator : Validator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginEndpoint(RidersDbContext db, JwtTokenService jwt) : Endpoint<LoginRequest, AuthResponse>
{
    public override void Configure() { Post("/riders/login"); AllowAnonymous(); }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var rider = await db.Riders.FirstOrDefaultAsync(r => r.Phone == req.Phone.Trim(), ct);
        if (rider is null || !rider.IsActive || !VerifyPassword(req.Password, rider.PasswordHash))
        {
            HttpContext.Response.StatusCode = 401;
            await HttpContext.Response.WriteAsJsonAsync(new { codigo = "riders.credenciales_invalidas", mensaje = "Teléfono o contraseña incorrectos." }, ct);
            return;
        }

        var (token, _) = jwt.CreateToken(rider);
        await Send.OkAsync(new AuthResponse { Token = token, Rider = RiderDto.From(rider) }, ct);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch (SaltParseException) { return false; }
    }
}

public sealed class MeEndpoint(RidersDbContext db, CurrentRider current) : EndpointWithoutRequest<RiderDto>
{
    public override void Configure() => Get("/riders/me");

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (current.RiderId is not { } id || await db.Riders.FindAsync([id], ct) is not { } rider)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(RiderDto.From(rider), ct);
    }
}

// ---------------- Editar perfil (nombre, zona, vehículo) ----------------

public sealed class UpdateProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
}

public sealed class UpdateProfileValidator : Validator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre es obligatorio.");
        RuleFor(x => x.Zone).NotEmpty().WithMessage("La zona donde operas es obligatoria.");
    }
}

public sealed class UpdateProfileEndpoint(RidersDbContext db, CurrentRider current) : Endpoint<UpdateProfileRequest, RiderDto>
{
    public override void Configure() => Put("/riders/me");

    public override async Task HandleAsync(UpdateProfileRequest req, CancellationToken ct)
    {
        if (current.RiderId is not { } id || await db.Riders.FindAsync([id], ct) is not { } rider)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        rider.Name = req.Name.Trim();
        rider.Zone = req.Zone.Trim();
        rider.VehicleType = req.VehicleType.Trim();
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(RiderDto.From(rider), ct);
    }
}

// ---------------- Cambiar contraseña ----------------

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordValidator : Validator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).MinimumLength(6).WithMessage("La nueva contraseña debe tener al menos 6 caracteres.");
    }
}

public sealed class ChangePasswordEndpoint(RidersDbContext db, CurrentRider current) : Endpoint<ChangePasswordRequest>
{
    public override void Configure() => Post("/riders/me/change-password");

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        if (current.RiderId is not { } id || await db.Riders.FindAsync([id], ct) is not { } rider)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        bool ok;
        try { ok = BCrypt.Net.BCrypt.Verify(req.CurrentPassword, rider.PasswordHash); }
        catch (SaltParseException) { ok = false; }

        if (!ok)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new { codigo = "riders.password_incorrecta", mensaje = "La contraseña actual no es correcta." }, ct);
            return;
        }

        rider.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
