using Ecommerce.Api.Data;
using Ecommerce.Api.Domain;
using MongoDB.Driver;

namespace Ecommerce.Api.Services;

public class OrderService
{
    private readonly Collections _c;
    public OrderService(Collections c) => _c = c;

    public async Task<Order> CreateAsync(string orderNumber, decimal amount, string currency)
    {
        var order = new Order { OrderNumber = orderNumber, Amount = amount, Currency = currency };
        await _c.Orders.InsertOneAsync(order);
        return order;
    }

    public async Task MarkPaidAsync(string orderNumber)
    {
        await _c.Orders.UpdateOneAsync(
            o => o.OrderNumber == orderNumber,
            Builders<Order>.Update.Set(o => o.Status, OrderStatus.Paid));
    }
}