using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ecommerce.Api.Domain;

public class PaymentEvent
{
    [BsonId] public ObjectId Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string Type { get; set; } = "payment.succeeded";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string RawBody { get; set; } = default!;
    public string Signature { get; set; } = default!;
}