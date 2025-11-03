using Ecommerce.Api.Data;
using Ecommerce.Api.Domain;
using MongoDB.Driver;

namespace Ecommerce.Api.Services;

public class IdempotencyService
{
    private readonly Collections _c;
    private readonly ILogger<IdempotencyService> _log;

    public IdempotencyService(Collections c, ILogger<IdempotencyService> log) { _c = c; _log = log; }

    public async Task<(bool exists, IdempotencyRecord? record)> TryGetAsync(string key)
    {
        try
        {
            var rec = await _c.Idempotency.Find(x => x.Key == key).FirstOrDefaultAsync();
            return (rec is not null, rec);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error checking idempotency key {Key}", key);
            return (false, null);
        }
    }

    public async Task SaveAsync(string key, int status, string responseBody)
    {
        try
        {
            await _c.Idempotency.InsertOneAsync(new IdempotencyRecord
            {
                Key = key,
                StatusCode = status,
                ResponseBody = responseBody
            });
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // concurrent duplicate; ignore
            _log.LogWarning("Idempotency key {Key} already stored", key);
        }
    }
}