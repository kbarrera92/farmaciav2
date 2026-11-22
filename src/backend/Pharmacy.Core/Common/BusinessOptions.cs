namespace Pharmacy.Core.Common;

/// <summary>Business-level settings (section "Business").</summary>
public sealed class BusinessOptions
{
    public const string SectionName = "Business";

    /// <summary>
    /// Offset from UTC of the place where the pharmacy operates, used to know "today" for date rules.
    /// Guatemala is UTC-6 all year (no daylight saving time), like the original system's offset.
    /// </summary>
    public double UtcOffsetHours { get; set; } = -6;
}
