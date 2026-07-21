namespace RidersHub.Security;

/// <summary>
/// Exige una API key válida (header <c>X-Api-Key</c>) para los endpoints que llama Comanda.Api
/// en nombre del restaurante (publicar job, enviar calificación). Las llaves se configuran en
/// secretos: `ApiKeys:0`, `ApiKeys:1`, … (mismo patrón que PaymentsHub).
/// </summary>
public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly HashSet<string> _keys =
        (config.GetSection("ApiKeys").Get<string[]>() ?? [])
        .Where(k => !string.IsNullOrWhiteSpace(k))
        .ToHashSet(StringComparer.Ordinal);

    private static readonly string[] Protected = ["/internal/"];

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        if (Protected.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            var provided = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
            if (provided is null || !_keys.Contains(provided))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { error = "API key inválida o ausente." });
                return;
            }
        }

        await next(ctx);
    }
}
