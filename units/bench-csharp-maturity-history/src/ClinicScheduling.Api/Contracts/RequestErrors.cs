namespace ClinicScheduling.Api.Contracts;

/// <summary>Collects field errors in the shape <c>Results.ValidationProblem</c> expects.</summary>
public sealed class RequestErrors
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    public bool Any => _errors.Count > 0;

    public RequestErrors Require(bool condition, string field, string message)
    {
        if (!condition)
        {
            if (!_errors.TryGetValue(field, out var messages))
            {
                messages = [];
                _errors[field] = messages;
            }

            messages.Add(message);
        }

        return this;
    }

    public Dictionary<string, string[]> ToDictionary() =>
        _errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.Ordinal);
}
