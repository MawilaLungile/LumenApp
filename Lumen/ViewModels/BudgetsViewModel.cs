namespace Lumen.ViewModels;

/// <summary>A budget joined with this month's actual spending.</summary>
public class BudgetItem
{
    private static readonly Color DangerColor = Color.FromArgb("#FF5C7A");
    private static readonly Color WarningColor = Color.FromArgb("#FFB547");

    public BudgetItem(Budget budget, double spent)
    {
        Budget = budget;
        Spent = spent;
        Category = Categories.Get(budget.CategoryId);
    }

    public Budget Budget { get; }
    public Category Category { get; }
    public double Spent { get; }
    public double Limit => Budget.Limit;

    public double Progress => Limit > 0 ? Spent / Limit : 0;
    public bool IsOver => Spent > Limit;

    public string SpentText => $"{Money.Format(Spent)} of {Money.Format(Limit)}";
    public string PercentText => Money.Percent(Progress);
    public string LeftText => IsOver
        ? $"⚠️ {Money.Format(Spent - Limit)} over budget"
        : $"{Money.Format(Limit - Spent)} left · {Money.Compact((Limit - Spent) / Math.Max(1, DaysLeft))}/day";

    public Color StatusColor => IsOver ? DangerColor : Progress > 0.8 ? WarningColor : Category.Color;

    private static int DaysLeft
    {
        get
        {
            var today = DateTime.Today;
            return DateTime.DaysInMonth(today.Year, today.Month) - today.Day + 1;
        }
    }
}

public partial class BudgetsViewModel : ObservableObject
{
    private readonly DatabaseService _db;

    public BudgetsViewModel(DatabaseService db)
    {
        _db = db;
    }

    [ObservableProperty] private List<BudgetItem> items = new();
    [ObservableProperty] private double overallProgress;
    [ObservableProperty] private string overallPercentText = "0%";
    [ObservableProperty] private string totalSpentText = string.Empty;
    [ObservableProperty] private string totalLimitText = string.Empty;
    [ObservableProperty] private string remainingText = string.Empty;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private bool isOverBudget;
    [ObservableProperty] private bool isRefreshing;

    public async Task LoadAsync()
    {
        var budgets = await _db.GetBudgetsAsync();
        var transactions = await _db.GetTransactionsAsync();

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var spentByCategory = transactions
            .Where(t => !t.IsIncome && t.Date >= monthStart)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        Items = budgets
            .Select(b => new BudgetItem(b, spentByCategory.GetValueOrDefault(b.CategoryId)))
            .OrderByDescending(i => i.Progress)
            .ToList();

        var limit = Items.Sum(i => i.Limit);
        var spent = Items.Sum(i => i.Spent);
        var remaining = limit - spent;

        OverallProgress = limit > 0 ? spent / limit : 0;
        OverallPercentText = Money.Percent(OverallProgress);
        TotalSpentText = Money.Format(spent);
        TotalLimitText = $"of {Money.Format(limit)} budgeted";
        IsOverBudget = remaining < 0;
        RemainingText = remaining >= 0 ? $"{Money.Format(remaining)} left to spend" : $"{Money.Format(-remaining)} over budget";

        var daysLeft = DateTime.DaysInMonth(today.Year, today.Month) - today.Day;
        StatusText = $"{daysLeft} days left in {today.ToString("MMMM", CultureInfo.InvariantCulture)}";
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task Add()
    {
        Haptics.Click();
        var used = Items.Select(i => i.Budget.CategoryId).ToHashSet();
        var options = Categories.Expenses.Where(c => !used.Contains(c.Id)).ToList();

        if (options.Count == 0)
        {
            await Dialogs.Alert("All set", "Every category already has a budget. Tap one to edit it.");
            return;
        }

        static string Label(Category c) => $"{c.Icon}  {c.Name}";

        var choice = await Dialogs.Sheet("Budget for which category?", null, options.Select(Label).ToArray());
        var category = options.FirstOrDefault(c => Label(c) == choice);
        if (category is null) return;

        var limit = await Dialogs.PromptAmount($"{category.Icon} {category.Name}", $"Monthly limit in {Money.Symbol}");
        if (limit is null) return;

        await _db.SaveBudgetAsync(new Budget { CategoryId = category.Id, Limit = limit.Value });
        await LoadAsync();
    }

    public async Task EditAsync(BudgetItem item)
    {
        Haptics.Click();
        var action = await Dialogs.Sheet($"{item.Category.Icon} {item.Category.Name}", "Delete budget", "Change limit");

        if (action == "Change limit")
        {
            var limit = await Dialogs.PromptAmount("New monthly limit", $"Amount in {Money.Symbol}", item.Limit);
            if (limit is null) return;

            item.Budget.Limit = limit.Value;
            await _db.SaveBudgetAsync(item.Budget);
            await LoadAsync();
        }
        else if (action == "Delete budget")
        {
            if (!await Dialogs.Confirm("Delete budget", $"Remove the {item.Category.Name} budget?", "Delete")) return;
            await _db.DeleteBudgetAsync(item.Budget);
            await LoadAsync();
        }
    }
}
