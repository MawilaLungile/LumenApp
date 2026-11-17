namespace Lumen.ViewModels;

/// <summary>A day's worth of transactions, used for the grouped list.</summary>
public class TransactionGroup : List<TransactionEntry>
{
    public TransactionGroup(string title, IEnumerable<TransactionEntry> items) : base(items)
    {
        Title = title;
        var net = this.Sum(t => t.SignedAmount);
        Total = (net >= 0 ? "+" : "−") + Money.Format(net);
    }

    public string Title { get; }

    public string Total { get; }
}

public partial class TransactionsViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private List<TransactionEntry> _all = new();

    public TransactionsViewModel(DatabaseService db)
    {
        _db = db;
    }

    [ObservableProperty] private string searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAll))]
    [NotifyPropertyChangedFor(nameof(IsIncomeFilter))]
    [NotifyPropertyChangedFor(nameof(IsExpenseFilter))]
    private string filter = "all";

    [ObservableProperty] private List<TransactionGroup> groups = new();
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string totalIn = string.Empty;
    [ObservableProperty] private string totalOut = string.Empty;
    [ObservableProperty] private bool isRefreshing;

    public bool IsAll => Filter == "all";
    public bool IsIncomeFilter => Filter == "income";
    public bool IsExpenseFilter => Filter == "expense";

    partial void OnSearchTextChanged(string value) => Apply();

    partial void OnFilterChanged(string value) => Apply();

    public async Task LoadAsync()
    {
        _all = await _db.GetTransactionsAsync();
        Apply();
    }

    private void Apply()
    {
        IEnumerable<TransactionEntry> query = _all;

        query = Filter switch
        {
            "income" => query.Where(t => t.IsIncome),
            "expense" => query.Where(t => !t.IsIncome),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            query = query.Where(t =>
                t.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                t.Category.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (t.Note?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var list = query.ToList();

        Groups = list
            .GroupBy(t => t.Date.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new TransactionGroup(FormatDay(g.Key), g.OrderByDescending(t => t.Date)))
            .ToList();

        TotalIn = Money.Compact(list.Where(t => t.IsIncome).Sum(t => t.Amount));
        TotalOut = Money.Compact(list.Where(t => !t.IsIncome).Sum(t => t.Amount));
        Summary = list.Count == 1 ? "1 transaction" : $"{list.Count} transactions";
    }

    private static string FormatDay(DateTime day)
    {
        if (day == DateTime.Today) return "Today";
        if (day == DateTime.Today.AddDays(-1)) return "Yesterday";
        return day.ToString("dddd, dd MMM", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void SetFilter(string value)
    {
        Haptics.Click();
        Filter = value;
    }

    [RelayCommand]
    private Task Add() => Shell.Current.GoToAsync(Routes.Editor);

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
        IsRefreshing = false;
    }

    public async Task DeleteAsync(TransactionEntry entry)
    {
        var ok = await Dialogs.Confirm("Delete transaction", $"Delete \"{entry.Title}\" ({entry.AmountDisplay})?", "Delete");
        if (!ok) return;

        await _db.DeleteTransactionAsync(entry);
        _all.Remove(entry);
        Haptics.LongPress();
        Apply();
    }
}
