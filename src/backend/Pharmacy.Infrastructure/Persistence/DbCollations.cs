namespace Pharmacy.Infrastructure.Persistence;

public static class DbCollations
{
    /// <summary>
    /// Case and accent insensitive with Spanish rules: "Analgésicos" = "ANALGESICOS", but "ñ" differs from "n".
    /// Set on the column so comparisons and unique indexes do not depend on the server default collation.
    /// </summary>
    public const string SpanishCaseAndAccentInsensitive = "Modern_Spanish_100_CI_AI";
}
