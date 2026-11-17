using SQLite;

namespace Lumen.Models;

[Table("Transactions")]
public class TransactionEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    public double Amount { get; set; }

    public bool IsIncome { get; set; }

    [Indexed]
    public string CategoryId { get; set; } = "food";

    [Indexed]
    public DateTime Date { get; set; } = DateTime.Now;

    public string? Note { get; set; }

    // ---------- Display helpers (not persisted) ----------

    [Ignore]
    public Category Category => Categories.Get(CategoryId);

    [Ignore]
    public string AmountDisplay => $"{(IsIncome ? "+" : "−")}{Money.Format(Amount)}";

    [Ignore]
    public string Subtitle => $"{Category.Name} · {Date.ToString("dd MMM, HH:mm", CultureInfo.InvariantCulture)}";

    [Ignore]
    public double SignedAmount => IsIncome ? Amount : -Amount;
}
