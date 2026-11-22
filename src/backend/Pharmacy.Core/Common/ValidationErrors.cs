namespace Pharmacy.Core.Common;

/// <summary>Collects field errors during a service validation and throws them together.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public int Count => _errors.Values.Sum(e => e.Count);

    public bool Has(string field) => _errors.ContainsKey(field);

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
            _errors[field] = list = [];
        list.Add(message);
    }

    public void ThrowIfAny()
    {
        if (_errors.Count > 0)
            throw new ValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }
}

/// <summary>Rules shared by every decimal(10,2) money column.</summary>
public static class MoneyRules
{
    /// <summary>Largest value that fits a decimal(10,2) column.</summary>
    public const decimal MaxMoney = 99_999_999.99m;

    public static void Validate(ValidationErrors errors, string field, string label, decimal value)
    {
        if (value <= 0)
            errors.Add(field, $"{label} debe ser mayor a 0.");
        else if (value > MaxMoney)
            errors.Add(field, $"{label} no puede superar {MaxMoney:N2}.");
        else if (decimal.Round(value, 2) != value)
            errors.Add(field, $"{label} admite como máximo 2 decimales.");
    }
}
