namespace Lumen.Models;

/// <summary>How much was spent in a category, and its share of the total.</summary>
public sealed record CategorySpend(Category Category, double Total, double Share)
{
    public string TotalText => Money.Format(Total);

    public string ShareText => Money.Percent(Share);
}
