using System.Globalization;
using System.Text.RegularExpressions;

namespace Pharmacy.Core.Common;

/// <summary>
/// Text rules shared by the application. Capitalization rules (decided by the user):
/// - Catalog names and descriptions: <see cref="ToSentenceCase"/> ("Analgésicos y antipiréticos").
/// - Proper names (people, company): <see cref="ToTitleCase"/> ("Kevin Barrera").
/// - Addresses: <see cref="CapitalizeFirst"/> (only the first letter is forced, the rest is kept).
/// In the first two rules, words of 2 to 4 letters written fully in upper case are kept as acronyms ("Pago POS").
/// </summary>
public static partial class TextNormalizer
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

    /// <summary>Trims the value and converts it to upper case for case-insensitive comparisons.</summary>
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    public static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>"ANALGÉSICOS Y antipiréticos" → "Analgésicos y antipiréticos"; "pago POS" → "Pago POS".</summary>
    public static string ToSentenceCase(string value)
    {
        var lowered = Word().Replace(value.Trim(), m => IsAcronym(m.Value) ? m.Value : m.Value.ToLower(Spanish));
        return CapitalizeFirst(lowered);
    }

    /// <summary>"KEVIN barrera" → "Kevin Barrera"; "ana pérez-molina" → "Ana Pérez-Molina".</summary>
    public static string ToTitleCase(string value) =>
        Word().Replace(value.Trim(), m => IsAcronym(m.Value)
            ? m.Value
            : string.Join('-', m.Value.ToLower(Spanish).Split('-').Select(CapitalizeFirst)));

    /// <summary>Upper-cases the first character when it is a letter; the rest is kept as written.</summary>
    public static string CapitalizeFirst(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length > 0 && char.IsLower(trimmed[0])
            ? char.ToUpper(trimmed[0], Spanish) + trimmed[1..]
            : trimmed;
    }

    public static string? ToSentenceCaseOrNull(string? value) =>
        TrimToNull(value) is { } text ? ToSentenceCase(text) : null;

    public static string? ToTitleCaseOrNull(string? value) =>
        TrimToNull(value) is { } text ? ToTitleCase(text) : null;

    public static string? CapitalizeFirstOrNull(string? value) =>
        TrimToNull(value) is { } text ? CapitalizeFirst(text) : null;

    private static bool IsAcronym(string word)
    {
        var letters = word.Where(char.IsLetter).ToList();
        return letters.Count is >= 2 and <= 4 && letters.All(char.IsUpper);
    }

    [GeneratedRegex(@"\S+")]
    private static partial Regex Word();
}
