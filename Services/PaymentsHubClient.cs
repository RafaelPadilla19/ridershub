using System.Net.Http.Json;

namespace RidersHub.Services;

public sealed record PaymentChargeResult(Guid ChargeId, string PayUrl);

/// <summary>
/// Cliente HTTP de PaymentsHub (el mismo microservicio que usa Comanda.Api; RidersHub es
/// otro consumidor más, con su propia API key y su propio callback). Config: Services:PaymentsHubUrl,
/// Services:SelfBaseUrl; secretos: Services:PaymentsApiKey, Payments:CallbackSecret.
/// </summary>
public sealed class PaymentsHubClient(HttpClient http, IConfiguration config, ILogger<PaymentsHubClient> logger)
{
    public async Task<PaymentChargeResult?> CreateSubscriptionChargeAsync(
        decimal amount, string description, string subscriptionRef, string riderId, string returnUrl, CancellationToken ct)
    {
        var selfBase = config["Services:SelfBaseUrl"] ?? "http://localhost:5061";
        var body = new
        {
            gateway = config["Services:PaymentsGateway"] ?? "fake",
            kind = "Subscription",
            amount,
            currency = "USD",
            description,
            tenantId = riderId,
            orderRef = subscriptionRef,
            returnUrl,
            callbackUrl = $"{selfBase}/internal/payments/callback",
            callbackKey = config["Payments:CallbackSecret"] ?? string.Empty,
        };

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/charges") { Content = JsonContent.Create(body) };
            req.Headers.Add("X-Api-Key", config["Services:PaymentsApiKey"] ?? string.Empty);

            var res = await http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("PaymentsHub respondió {Status} al crear el cobro de suscripción {Ref}", res.StatusCode, subscriptionRef);
                return null;
            }

            var dto = await res.Content.ReadFromJsonAsync<ChargeResponse>(ct);
            return dto is null ? null : new PaymentChargeResult(dto.Id, dto.PayUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error llamando a PaymentsHub (suscripción {Ref})", subscriptionRef);
            return null;
        }
    }

    private sealed record ChargeResponse(Guid Id, string PayUrl);
}
