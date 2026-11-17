using SQLite;

namespace Lumen.Models;

[Table("Goals")]
public class SavingsGoal
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = "⭐";

    public double Target { get; set; }

    public double Saved { get; set; }

    public string ColorHex { get; set; } = "#7C5CFF";

    public DateTime Deadline { get; set; } = DateTime.Today.AddMonths(6);

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ---------- Display helpers (not persisted) ----------

    [Ignore]
    public double Progress => Target <= 0 ? 0 : Math.Clamp(Saved / Target, 0, 1);

    [Ignore]
    public bool IsComplete => Target > 0 && Saved >= Target;

    [Ignore]
    public Color Color => Color.FromArgb(ColorHex);

    [Ignore]
    public Color SoftColor => Color.FromArgb(ColorHex).WithAlpha(0.18f);

    [Ignore]
    public string SavedText => Money.Format(Saved);

    [Ignore]
    public string TargetText => $"of {Money.Format(Target)}";

    [Ignore]
    public string PercentText => Money.Percent(Progress);

    [Ignore]
    public string DeadlineText
    {
        get
        {
            if (IsComplete) return "Completed 🎉";
            var days = (Deadline.Date - DateTime.Today).Days;
            return days switch
            {
                < 0 => "Past deadline",
                0 => "Due today",
                1 => "1 day left",
                _ => $"{days} days left"
            };
        }
    }

    [Ignore]
    public string MonthlyNeededText
    {
        get
        {
            if (IsComplete) return "Target reached";
            var months = Math.Max(1, ((Deadline.Year - DateTime.Today.Year) * 12) + Deadline.Month - DateTime.Today.Month);
            return $"{Money.Compact((Target - Saved) / months)}/mo to finish";
        }
    }
}
