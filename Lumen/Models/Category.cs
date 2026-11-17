namespace Lumen.Models;

/// <summary>An immutable spending / income category.</summary>
public sealed record Category(string Id, string Name, string Icon, string ColorHex, bool IsIncome)
{
    public Color Color => Color.FromArgb(ColorHex);

    /// <summary>A translucent version of the category colour, used for icon backgrounds.</summary>
    public Color SoftColor => Color.FromArgb(ColorHex).WithAlpha(0.18f);

    public string ShortName => Name.Split(' ')[0];
}

public static class Categories
{
    public static readonly IReadOnlyList<Category> All = new List<Category>
    {
        // Expenses
        new("food",          "Food & Dining",     "🍔", "#FF6B6B", false),
        new("transport",     "Transport",         "🚗", "#4ECDC4", false),
        new("shopping",      "Shopping",          "🛍️", "#FFB86B", false),
        new("bills",         "Bills & Utilities", "💡", "#A78BFA", false),
        new("entertainment", "Entertainment",     "🎬", "#F472B6", false),
        new("health",        "Health",            "💊", "#34D399", false),
        new("education",     "Education",         "📚", "#60A5FA", false),
        new("travel",        "Travel",            "✈️", "#FBBF24", false),

        // Income
        new("salary",        "Salary",            "💼", "#22C55E", true),
        new("freelance",     "Freelance",         "💻", "#10B981", true),
        new("investment",    "Investments",       "📈", "#14B8A6", true),
        new("gift",          "Gifts",             "🎁", "#E879F9", true),
    };

    public static IEnumerable<Category> Expenses => All.Where(c => !c.IsIncome);

    public static IEnumerable<Category> Income => All.Where(c => c.IsIncome);

    public static Category Get(string? id) => All.FirstOrDefault(c => c.Id == id) ?? All[0];
}
