using System.Security.Cryptography;
using System.Text;

namespace Shipping.Rates.Core.Accounts;

public sealed class CarrierAccountManager
{
    private readonly Dictionary<string, string> _apiKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _keyRotatedAt = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _requestCounts = new(StringComparer.Ordinal);
    private readonly int _requestsPerMinute;
    private DateTimeOffset m_windowStart;
    private readonly List<string> _auditTrail = [];
    private readonly int _auditCapacity;
    private readonly byte[] _webhookSecret;
    private readonly string _signaturePrefix;

    public CarrierAccountManager(int requestsPerMinute, int auditCapacity, byte[] webhookSecret)
    {
        _requestsPerMinute = requestsPerMinute;
        _auditCapacity = auditCapacity;
        _webhookSecret = webhookSecret;
        _signaturePrefix = "sha256=";
    }

    // stores the key and remembers when it was rotated
    public void RotateKey(string carrier, string apiKey, DateTimeOffset now)
    {
        _apiKeys[carrier] = apiKey;
        _keyRotatedAt[carrier] = now;
    }

    public string? KeyFor(string carrier)
    {
        var found = _apiKeys.TryGetValue(carrier, out var key);
        return found ? key : null;
    }

    public bool IsKeyStale(string carrier, DateTimeOffset now, TimeSpan maxAge)
    {
        if (!_keyRotatedAt.TryGetValue(carrier, out var rotated))
        {
            return _apiKeys.ContainsKey(carrier);
        }

        return now - rotated > maxAge;
    }

    public bool TryConsumeRequest(string carrier, DateTimeOffset now)
    {
        if (now - m_windowStart >= TimeSpan.FromMinutes(1))
        {
            m_windowStart = now;
            _requestCounts.Clear();
        }

        _requestCounts.TryGetValue(carrier, out var used);
        if (used >= _requestsPerMinute)
        {
            return false;
        }

        // increment the counter
        _requestCounts[carrier] = used + 1;
        return true;
    }

    public int RequestsUsed(string carrier)
    {
        _requestCounts.TryGetValue(carrier, out var used);
        return Math.Min(used, _requestsPerMinute);
    }

    public void RecordAudit(string entry, DateTimeOffset now)
    {
        _auditTrail.Add($"{now:O} {entry}");
        if (_auditTrail.Count > _auditCapacity)
        {
            _auditTrail.RemoveAt(0);
        }
    }

    public IReadOnlyList<string> RecentAudit(int count)
    {
        var take = Math.Min(count, _auditCapacity);
        return _auditTrail.TakeLast(take).ToList();
    }

    public string SignWebhook(string payload)
    {
        var mac = HMACSHA256.HashData(_webhookSecret, Encoding.UTF8.GetBytes(payload));
        return _signaturePrefix + Convert.ToHexStringLower(mac);
    }

    public bool VerifyWebhook(string payload, string? signature)
    {
        if (signature is null || !signature.StartsWith(_signaturePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var expected = Encoding.ASCII.GetBytes(SignWebhook(payload));
        return CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(signature));
    }
}
