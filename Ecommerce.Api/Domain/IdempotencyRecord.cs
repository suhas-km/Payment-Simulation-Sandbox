using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ecommerce.Api.Domain;

public class IdempotencyRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    [BsonElement("key")]
    [BsonRequired]
    public string Key { get; set; } = default!;
    
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}