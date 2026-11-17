namespace Lumen.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;

    public DashboardViewModel(DatabaseService db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;

    [ObservableProperty] private string greeting = string.Empty;
    [ObservableProperty] private string userName = string.Empty;
    [ObservableProperty] private string initials = string.Empty;

    [ObservableProperty] private string balance = string.Empty;
    [ObservableProperty] private string monthLabel = string.Empty;
    [ObservableProperty] private string monthIncome = string.Empty;
    [ObservableProperty] private string monthExpense = string.Empty;
    [ObservableProperty] private string savingsRateText = string.Empty;

    [ObservableProperty] private string insight = string.Empty;
    [ObservableProperty] private string insightEmoji = "💡";

    [ObservableProperty] private string weekTotal = string.Empty;
    [ObservableProperty] private IList<double> weeklyValues = new List<double>();
    [ObservableProperty] private IList<string> weeklyLabels = new List<string>();

    [ObservableProperty] private IList<ChartSlice> categorySlices = new List<ChartSlice>();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoCategoryData))]
    private bool hasCategoryData;

    public bool HasNoCategoryData => !HasCategoryData;

    [ObservableProperty] private bool hasRecent;
    [ObservableProperty] private bool hasNoRecent;

    public ObservableCollection<CategorySpend> TopCategories { get; } = new();

    public ObservableCollection<TransactionEntry> RecentTransactions { get; } = new();

    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var all = await _db.GetTransactionsAsync();
            var now = DateTime.Now;
            var today = DateTime.Today;
            var ci = CultureInfo.InvariantCulture;

            // ---- Header
            Greeting = now.Hour switch
            {
                < 12 => "Good morning ☀️",
                < 17 => "Good afternoon 🌤️",
                _ => "Good evening 🌙"
            };
            UserName = _settings.UserName;
            Initials = SettingsService.GetInitials(UserName);

            // ---- Balance & this month
            var totalIncome = all.Where(t => t.IsIncome).Sum(t => t.Amount);
            var totalExpense = all.Where(t => !t.IsIncome).Sum(t => t.Amount);
            Balance = Money.FormatSigned(totalIncome - totalExpense);

            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthly = all.Where(t => t.Date >= monthStart).ToList();
            var mIncome = monthly.Where(t => t.IsIncome).Sum(t => t.Amount);
            var mExpense = monthly.Where(t => !t.IsIncome).Sum(t => t.Amount);

            MonthLabel = today.ToString("MMMM yyyy", ci);
            MonthIncome = Money.Format(mIncome);
            MonthExpense = Money.Format(mExpense);

            if (mIncome > 0)
            {
                var rate = (mIncome - mExpense) / mIncome;
                SavingsRateText = rate >= 0
                    ? $"▲ You've kept {Money.Percent(rate)} of this month's income"
                    : $"▼ You've spent {Money.Percent(-rate)} more than you earned";
            }
            else
            {
                SavingsRateText = "Add income to see your savings rate";
            }

            // ---- Last 7 days
            var days = Enumerable.Range(0, 7).Select(i => today.AddDays(i - 6)).ToList();
            var weekly = days.Select(d => all.Where(t => !t.IsIncome && t.Date.Date == d).Sum(t => t.Amount)).ToList();
            WeeklyLabels = days.Select(d => d == today ? "Today" : d.ToString("ddd", ci)).ToList();
            WeeklyValues = weekly;
            WeekTotal = Money.Format(weekly.Sum());

            // ---- Categories this month
            var byCategory = monthly
                .Where(t => !t.IsIncome)
                .GroupBy(t => t.CategoryId)
                .Select(g => new { Category = Categories.Get(g.Key), Total = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.Total)
                .ToList();

            CategorySlices = byCategory.Select(x => new ChartSlice(x.Category.Name, x.Total, x.Category.Color)).ToList();
            HasCategoryData = byCategory.Count > 0;

            TopCategories.Clear();
            foreach (var c in byCategory.Take(5))
                TopCategories.Add(new CategorySpend(c.Category, c.Total, mExpense > 0 ? c.Total / mExpense : 0));

            // ---- Insight of the day
            BuildInsight(all, weekly.Sum(), today, byCategory.FirstOrDefault()?.Category);

            // ---- Recent
            RecentTransactions.Clear();
            foreach (var t in all.Take(5))
                RecentTransactions.Add(t);
            HasRecent = RecentTransactions.Count > 0;
            HasNoRecent = !HasRecent;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildInsight(List<TransactionEntry> all, double thisWeek, DateTime today, Category? topCategory)
    {
        var lastWeek = all
            .Where(t => !t.IsIncome && t.Date.Date >= today.AddDays(-13) && t.Date.Date <= today.AddDays(-7))
            .Sum(t => t.Amount);

        if (lastWeek > 0)
        {
            var change = (thisWeek - lastWeek) / lastWeek;
            if (change <= 0)
            {
                InsightEmoji = "🎉";
                Insight = $"You spent {Money.Percent(Math.Abs(change))} less than last week. Keep the momentum going!";
            }
            else
            {
                InsightEmoji = "📈";
                Insight = topCategory is null
                    ? $"Spending is up {Money.Percent(change)} compared to last week."
                    : $"Spending is up {Money.Percent(change)} vs last week. {topCategory.Name} is your biggest category this month.";
            }
        }
        else
        {
            InsightEmoji = "💡";
            Insight = "Log a few transactions and Lumen will surface personalised insights here.";
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task AddTransaction()
    {
        Haptics.Click();
        await Shell.Current.GoToAsync(Routes.Editor);
    }

    [RelayCommand]
    private Task SeeAll() => Shell.Current.GoToAsync(Routes.Activity);
}
