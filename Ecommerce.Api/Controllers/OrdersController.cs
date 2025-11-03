using System.Text.Json;
using Ecommerce.Api.Domain;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IdempotencyService _idemp;
    private readonly OrderService _orders;
    private readonly OutboxChannel _outbox;
    private const string IdempotencyHeader = "Idempotency-Key";

    public OrdersController(IdempotencyService idemp, OrderService orders, OutboxChannel outbox)
    {
        _idemp = idemp; _orders = orders; _outbox = outbox;
    }

    public record CreateOrderRequest(string OrderNumber, decimal Amount, string Currency = "USD");

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        if (!Request.Headers.TryGetValue(IdempotencyHeader, out var key) || string.IsNullOrWhiteSpace(key))
            return BadRequest(new { error = $"Missing {IdempotencyHeader} header" });

        // Check idempotency
        var (exists, rec) = await _idemp.TryGetAsync(key!);
        if (exists && rec is not null)
        {
            Response.StatusCode = rec.StatusCode;
            return Content(rec.ResponseBody, "application/json");
        }

        // Create order (first time)
        var order = await _orders.CreateAsync(req.OrderNumber, req.Amount, req.Currency);

        // Enqueue payment simulation
        await _outbox.Channel.Writer.WriteAsync(new PaymentSimulationJob(order.OrderNumber, order.Amount, order.Currency));

        var response = new
        {
            order.Id,
            order.OrderNumber,
            order.Amount,
            order.Currency,
            order.Status,
            order.CreatedAt
        };
        var json = JsonSerializer.Serialize(response);

        await _idemp.SaveAsync(key!, 201, json);
        return Created($"/api/orders/{order.OrderNumber}", response);
    }
}
