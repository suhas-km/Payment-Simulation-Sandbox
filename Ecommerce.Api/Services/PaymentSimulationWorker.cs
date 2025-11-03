using System.Text;
using System.Text.Json;

namespace Ecommerce.Api.Services;

public class PaymentSimulationWorker : BackgroundService
{
    private readonly OutboxChannel _outbox;
    private readonly ILogger<PaymentSimulationWorker> _log;
    private readonly IHttpClientFactory _http;
    private readonly WebhookSigner _signer;
    private readonly IConfiguration _cfg;

    public PaymentSimulationWorker(OutboxChannel outbox,
                                   ILogger<PaymentSimulationWorker> log,
                                   IHttpClientFactory http,
                                   WebhookSigner signer,
                                   IConfiguration cfg)
    {
        _outbox = outbox; _log = log; _http = http; _signer = signer; _cfg = cfg;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = _http.CreateClient("webhook");
        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await _outbox.Channel.Reader.ReadAsync(stoppingToken);

            // simulate processing time
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            var payload = new
            {
                type = "payment.succeeded",
                data = new { orderNumber = job.OrderNumber, amount = job.Amount, currency = job.Currency }
            };

            var body = JsonSerializer.Serialize(payload);
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signature = _signer.ComputeSignature(body, ts);

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/payments")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            req.Headers.Add(_signer.HeaderName, signature);

            var baseUrl = _cfg["Webhook:BaseUrl"] ?? "http://localhost:5000"; // HTTP is fine locally
            req.RequestUri = new Uri($"{baseUrl}/api/webhooks/payments");

            var resp = await client.SendAsync(req, stoppingToken);
            _log.LogInformation("Webhook POST returned {Status}", resp.StatusCode);
        }
    }
}