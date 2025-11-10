using System.Text.Json;
using Ecommerce.Api.Domain;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

// [ApiController] - attribute that marks the class as a controller
// [Route("api/[controller]")] - attribute that defines the base route for the controller
[ApiController]
[Route("api/[controller]")]
// OrdersController inherits from ControllerBase? 
// ControllerBase is a base class for controllers
// OrdersController is a controller that handles HTTP requests and responses


public class OrdersController : ControllerBase
{
    private readonly IdempotencyService _idemp;
    private readonly OrderService _orders;
    // OutboxChannel is a class that is used to enqueue jobs
    private readonly OutboxChannel _outbox;
    private const string IdempotencyHeader = "Idempotency-Key";

    public OrdersController(IdempotencyService idemp, OrderService orders, OutboxChannel outbox)
    {
        _idemp = idemp; _orders = orders; _outbox = outbox;
    }

    public record CreateOrderRequest(string OrderNumber, decimal Amount, string Currency = "USD");

    // [HttpPost] - attribute that marks the method as a POST endpoint
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        // Idempotency check
        // Idempotency is a property of a request that ensures that it can be repeated multiple times without different results
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
