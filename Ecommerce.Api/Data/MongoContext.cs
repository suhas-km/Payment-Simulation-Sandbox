using MongoDB.Driver;
using Ecommerce.Api.Domain;

namespace Ecommerce.Api.Data;

public class MongoContext
{
    public IMongoDatabase Db { get; }
    public MongoContext(IConfiguration cfg)
    {
        var cs = cfg.GetSection("Mongo:ConnectionString").Get<string>()!;
        var dbName = cfg.GetSection("Mongo:Database").Get<string>()!;
        var client = new MongoClient(cs);
        Db = client.GetDatabase(dbName);

        // Get collections
        var orders = Db.GetCollection<Order>("orders");
        var payments = Db.GetCollection<PaymentEvent>("payments");
        var idempotency = Db.GetCollection<IdempotencyRecord>("idempotency");

        // Create indexes
        idempotency.Indexes.CreateOne(
            new CreateIndexModel<IdempotencyRecord>(
                Builders<IdempotencyRecord>.IndexKeys.Ascending(x => x.Key),
                new CreateIndexOptions { Unique = true, Name = "key_unique" }
            )
        );
    }
}