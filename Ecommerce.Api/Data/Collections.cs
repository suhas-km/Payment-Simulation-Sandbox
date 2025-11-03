using MongoDB.Driver;
using Ecommerce.Api.Domain;

namespace Ecommerce.Api.Data;

public class Collections
{
    public IMongoCollection<Order> Orders { get; }
    public IMongoCollection<PaymentEvent> Payments { get; }
    public IMongoCollection<IdempotencyRecord> Idempotency { get; }

    public Collections(MongoContext ctx)
    {
        Orders = ctx.Db.GetCollection<Order>("orders");
        Payments = ctx.Db.GetCollection<PaymentEvent>("payments");
        Idempotency = ctx.Db.GetCollection<IdempotencyRecord>("idempotency");
    }
}