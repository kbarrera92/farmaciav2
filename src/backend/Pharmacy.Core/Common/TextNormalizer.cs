namespace Pharmacy.Core.Common;

public static class TextNormalizer
{
    /// <summary>Trims the value and converts it to upper case for case-insensitive comparisons.</summary>
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    public static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
