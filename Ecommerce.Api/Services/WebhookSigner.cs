using System.Security.Cryptography;
using System.Text;

namespace Ecommerce.Api.Services;

public class WebhookSigner
{
    private readonly string _secret;
    private readonly int _toleranceSeconds;
    public readonly string HeaderName;

    public WebhookSigner(IConfiguration cfg)
    {
        _secret = cfg["Webhook:SharedSecret"]!;
        HeaderName = cfg["Webhook:SignatureHeader"] ?? "X-Signature";
        _toleranceSeconds = int.TryParse(cfg["Webhook:ToleranceSeconds"], out var t) ? t : 300;
    }

    public string ComputeSignature(string body, long timestamp)
    {
        var payload = $"t={timestamp}.{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var sig = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={timestamp},v1={sig}";
    }

    public bool Verify(string? header, string body, out string reason)
    {
        reason = "";
        if (string.IsNullOrWhiteSpace(header)) { reason = "Missing signature"; return false; }

        // parse t and v1
        var parts = header.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var tPart = parts.FirstOrDefault(p => p.StartsWith("t="));
        var v1Part = parts.FirstOrDefault(p => p.StartsWith("v1="));

        if (tPart is null || v1Part is null) { reason = "Malformed signature"; return false; }

        if (!long.TryParse(tPart.Split('=')[1], out var ts)) { reason = "Bad timestamp"; return false; }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - ts) > _toleranceSeconds) { reason = "Stale timestamp"; return false; }

        var expected = ComputeSignature(body, ts);
        var expectedV1 = expected.Split(',').First(p => p.StartsWith("v1=")).Split('=')[1];
        var providedV1 = v1Part.Split('=')[1];

        var ok = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedV1),
            Encoding.UTF8.GetBytes(providedV1));

        if (!ok) reason = "Signature mismatch";
        return ok;
    }
}