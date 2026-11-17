namespace Lumen.Services;

/// <summary>Currency formatting helpers shared by models, view models and charts.</summary>
public static class Money
{
    public static string Symbol { get; set; } = "R";

    public static string Format(double value) =>
        $"{Symbol}{Math.Abs(value).ToString("N2", CultureInfo.InvariantCulture)}";

    public static string FormatSigned(double value) =>
        (value < 0 ? "−" : string.Empty) + Format(value);

    public static string Compact(double value)
    {
        var v = Math.Abs(value);
        var sign = value < 0 ? "−" : string.Empty;
        return v switch
        {
            >= 1_000_000 => $"{sign}{Symbol}{(v / 1_000_000).ToString("0.#", CultureInfo.InvariantCulture)}M",
            >= 1_000 => $"{sign}{Symbol}{(v / 1_000).ToString("0.#", CultureInfo.InvariantCulture)}k",
            _ => $"{sign}{Symbol}{v.ToString("0", CultureInfo.InvariantCulture)}"
        };
    }

    public static string Percent(double fraction) =>
        $"{Math.Round(fraction * 100).ToString("0", CultureInfo.InvariantCulture)}%";

    /// <summary>Lenient parser: accepts "1200", "1 200,50", "R1200.5".</summary>
    public static bool TryParse(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var cleaned = string.IsNullOrEmpty(Symbol) ? text : text.Replace(Symbol, string.Empty);
        cleaned = cleaned.Replace(" ", string.Empty)
                          .Replace(" ", string.Empty)
                          .Replace(",", ".");

        // If there are several dots (thousand separators), keep only the last as decimal point.
        var lastDot = cleaned.LastIndexOf('.');
        if (lastDot >= 0 && cleaned.IndexOf('.') != lastDot)
            cleaned = cleaned[..lastDot].Replace(".", string.Empty) + cleaned[lastDot..];

        return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
