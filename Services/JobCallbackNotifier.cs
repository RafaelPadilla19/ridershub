using System.Net.Http.Json;
using RidersHub.Domain;

namespace RidersHub.Services;

/// <summary>Avisa a Comanda.Api cuando un job del pool cambia de estado (aceptado/entregado). Mismo patrón que CallbackNotifier de PaymentsHub.</summary>
public sealed class JobCallbackNotifier(IHttpClientFactory http, ILogger<JobCallbackNotifier> log)
{
    public async Task NotifyAsync(DeliveryJob job, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(job.CallbackUrl)) return;
        try
        {
            var client = http.CreateClient();
            using var msg = new HttpRequestMessage(HttpMethod.Post, job.CallbackUrl);
            if (!string.IsNullOrWhiteSpace(job.CallbackKey))
                msg.Headers.Add("X-Callback-Key", job.CallbackKey);
            msg.Content = JsonContent.Create(new
            {
                jobId = job.Id,
                orderId = job.OrderId,
                status = job.Status.ToString(),
                riderName = job.RiderName,
            });
            await client.SendAsync(msg, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo notificar el callback del job {JobId} a {Url}", job.Id, job.CallbackUrl);
        }
    }
}
