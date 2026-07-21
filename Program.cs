using System.Text;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RidersHub.Persistence;
using RidersHub.Security;
using RidersHub.Services;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Riders")
    ?? throw new InvalidOperationException("Falta la cadena 'Riders' (configúrala en user-secrets o ConnectionStrings__Riders).");
builder.Services.AddDbContext<RidersDbContext>(o => o.UseNpgsql(cs));

// ---- Autenticación JWT (riders) ----
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["SigningKey"]
    ?? throw new InvalidOperationException("Falta 'Jwt:SigningKey' (configúralo por variable de entorno Jwt__SigningKey).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // Sin esto, ASP.NET renombra "sub" al URI largo ClaimTypes.NameIdentifier al validar
        // el token, y CurrentRider (que busca "sub" tal cual) nunca lo encuentra.
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"] ?? "RidersHub",
            ValidateAudience = true,
            ValidAudience = jwt["Audience"] ?? "RidersHubClients",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<CurrentRider>();

// ---- Cliente de PaymentsHub (mismo microservicio que usa Comanda.Api) ----
var paymentsUrl = builder.Configuration["Services:PaymentsHubUrl"] ?? "http://localhost:5060";
builder.Services.AddHttpClient<PaymentsHubClient>(c => c.BaseAddress = new Uri(paymentsUrl));
builder.Services.AddScoped<JobCallbackNotifier>();

// ---- FastEndpoints + Swagger ----
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.EnableJWTBearerAuth = true;
    o.DocumentSettings = s => { s.Title = "RidersHub"; s.Version = "v1"; s.Description = "Pool de riders independientes de Comanda."; };
});

var corsOrigins = (builder.Configuration["Cors:Origins"] ?? "http://localhost:4400")
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddPolicy("riders-client", p => p
    .WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<RidersDbContext>().Database.MigrateAsync();

app.UseCors("riders-client");
app.UseAuthentication();
app.UseMiddleware<RidersHub.Security.ApiKeyMiddleware>();
app.UseAuthorization();
app.UseFastEndpoints(c => c.Serializer.Options.Converters.Add(new JsonStringEnumConverter()));
app.UseSwaggerGen();

app.Run();
