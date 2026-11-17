using SQLite;

namespace Lumen.Services;

/// <summary>
/// Generates ~2.5 months of realistic demo data so the app looks alive on first launch.
/// Deterministic (fixed seed) so screenshots are reproducible.
/// </summary>
public static class SeedData
{
    private record ExpenseTemplate(string Category, string[] Titles, double Min, double Max, double DailyChance);

    private static readonly ExpenseTemplate[] Templates =
    {
        new("food", new[] { "Woolworths Food", "Checkers", "Uber Eats", "Vida e Caffè", "Nando's", "Pick n Pay", "Kauai" }, 45, 380, 0.70),
        new("transport", new[] { "Uber ride", "Fuel – Engen", "Gautrain", "Bolt ride", "Parking" }, 35, 600, 0.40),
        new("shopping", new[] { "Takealot order", "Mr Price", "Zara", "Cotton On", "iStore" }, 120, 1400, 0.10),
        new("entertainment", new[] { "Netflix", "Spotify Premium", "Ster-Kinekor", "Concert tickets", "Steam" }, 60, 450, 0.12),
        new("health", new[] { "Virgin Active", "Clicks pharmacy", "Dis-Chem" }, 80, 600, 0.08),
        new("education", new[] { "Udemy course", "Pluralsight", "Exclusive Books" }, 150, 900, 0.05),
    };

    public static async Task SeedAsync(SQLiteAsyncConnection db)
    {
        var rnd = new Random(2026);
        var now = DateTime.Now;
        var today = DateTime.Today;
        var list = new List<TransactionEntry>();

        // Daily variable spending for the last 75 days.
        for (var d = 0; d < 75; d++)
        {
            var day = today.AddDays(-d);
            foreach (var t in Templates)
            {
                if (rnd.NextDouble() >= t.DailyChance) continue;

                var when = day.AddHours(7 + rnd.Next(14)).AddMinutes(rnd.Next(60));
                if (when > now) when = now.AddMinutes(-rnd.Next(5, 120));

                list.Add(new TransactionEntry
                {
                    Title = t.Titles[rnd.Next(t.Titles.Length)],
                    Amount = Math.Round(t.Min + rnd.NextDouble() * (t.Max - t.Min), 2),
                    CategoryId = t.Category,
                    Date = when,
                    IsIncome = false
                });
            }
        }

        // Recurring monthly items for the last 3 months.
        for (var m = 0; m < 3; m++)
        {
            var month = new DateTime(today.Year, today.Month, 1).AddMonths(-m);

            void AddIfPast(string title, double amount, string cat, bool income, int dayOfMonth, int hour)
            {
                var date = new DateTime(month.Year, month.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(month.Year, month.Month)), hour, 0, 0);
                if (date <= now)
                    list.Add(new TransactionEntry { Title = title, Amount = amount, CategoryId = cat, IsIncome = income, Date = date });
            }

            AddIfPast("Monthly salary – Contoso", 38_500, "salary", true, 1, 8);
            AddIfPast("Rent", 9_500, "bills", false, 1, 10);
            AddIfPast("Prepaid electricity", 1_150, "bills", false, 3, 18);
            AddIfPast("Fibre – Afrihost", 699, "bills", false, 5, 9);
            AddIfPast("Phone contract", 449, "bills", false, 7, 9);
            AddIfPast("Freelance – mobile UI project", Math.Round(6_500 + rnd.NextDouble() * 5_500, 2), "freelance", true, 12, 15);
        }

        list.Add(new TransactionEntry { Title = "Dividend – Satrix ETF", Amount = 842.60, CategoryId = "investment", IsIncome = true, Date = today.AddDays(-19).AddHours(11) });
        list.Add(new TransactionEntry { Title = "Birthday gift from Gran", Amount = 1_000, CategoryId = "gift", IsIncome = true, Date = today.AddDays(-33).AddHours(16) });
        list.Add(new TransactionEntry { Title = "Flight – Joburg to Cape Town", Amount = 2_340, CategoryId = "travel", IsIncome = false, Date = today.AddDays(-27).AddHours(13), Note = "Holiday in December" });

        await db.InsertAllAsync(list);

        await db.InsertAllAsync(new[]
        {
            new Budget { CategoryId = "food", Limit = 5_000 },
            new Budget { CategoryId = "transport", Limit = 4_000 },
            new Budget { CategoryId = "shopping", Limit = 2_500 },
            new Budget { CategoryId = "bills", Limit = 12_500 },
            new Budget { CategoryId = "entertainment", Limit = 800 },
            new Budget { CategoryId = "health", Limit = 1_200 },
        });

        await db.InsertAllAsync(new[]
        {
            new SavingsGoal { Name = "Emergency fund", Icon = "🛟", Target = 60_000, Saved = 38_400, ColorHex = "#7C5CFF", Deadline = today.AddMonths(8), CreatedAt = today.AddMonths(-10) },
            new SavingsGoal { Name = "New MacBook Pro", Icon = "💻", Target = 35_000, Saved = 14_200, ColorHex = "#00B4D8", Deadline = today.AddMonths(5), CreatedAt = today.AddMonths(-3) },
            new SavingsGoal { Name = "Cape Town getaway", Icon = "🏖️", Target = 12_000, Saved = 9_800, ColorHex = "#FFB547", Deadline = today.AddDays(40), CreatedAt = today.AddMonths(-4) },
            new SavingsGoal { Name = "Car deposit", Icon = "🚗", Target = 50_000, Saved = 6_500, ColorHex = "#F472B6", Deadline = today.AddMonths(18), CreatedAt = today.AddMonths(-1) },
        });
    }
}
