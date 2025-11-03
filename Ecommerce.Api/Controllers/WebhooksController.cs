using System.Text;
using Ecommerce.Api.Data;
using Ecommerce.Api.Domain;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/webhooks/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly WebhookSigner _signer;
    private readonly Collections _c;
    private readonly OrderService _orders;

    public PaymentsController(WebhookSigner signer, Collections c, OrderService orders)
    {
        _signer = signer; _c = c; _orders = orders;
    }

    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        var header = Request.Headers[_signer.HeaderName].FirstOrDefault();
        if (!_signer.Verify(header, body, out var reason))
            return Unauthorized(new { error = "Invalid signature", reason });

        // store the raw event
        await _c.Payments.InsertOneAsync(new PaymentEvent
        {
            RawBody = body,
            Signature = header!
        });

        // parse minimal fields
        var doc = System.Text.Json.JsonDocument.Parse(body);
        var orderNumber = doc.RootElement.GetProperty("data").GetProperty("orderNumber").GetString()!;
        await _orders.MarkPaidAsync(orderNumber);

        return Ok(new { ok = true });
    }
}