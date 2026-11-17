using System.Text;

namespace Lumen.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;

    public SettingsViewModel(DatabaseService db, SettingsService settings)
    {
        _db = db;
        _settings = settings;

        userName = settings.UserName;
        initials = SettingsService.GetInitials(userName);
        themeIndex = settings.Theme;
        currency = settings.Currency;
    }

    public List<string> Themes { get; } = new() { "System", "Light", "Dark" };

    public List<string> Currencies { get; } = new() { "R", "$", "€", "£", "₦", "KSh", "₹", "¥" };

    public string AppVersion => $"Version {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

    public string PlatformText => $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString} · {DeviceInfo.Current.Idiom}";

    [ObservableProperty] private string userName;
    [ObservableProperty] private string initials;
    [ObservableProperty] private int themeIndex;
    [ObservableProperty] private string currency;
    [ObservableProperty] private string statsText = string.Empty;

    partial void OnUserNameChanged(string value)
    {
        _settings.UserName = string.IsNullOrWhiteSpace(value) ? "Friend" : value.Trim();
        Initials = SettingsService.GetInitials(value);
    }

    partial void OnThemeIndexChanged(int value)
    {
        if (value < 0) return;
        _settings.Theme = value;
        _settings.ApplyTheme();
    }

    partial void OnCurrencyChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        _settings.Currency = value;
        Money.Symbol = value;
    }

    public async Task LoadAsync()
    {
        var tx = await _db.GetTransactionsAsync();
        var goals = await _db.GetGoalsAsync();
        StatsText = $"{tx.Count} transactions · {goals.Count} goals";
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        var tx = await _db.GetTransactionsAsync();
        if (tx.Count == 0)
        {
            await Dialogs.Alert("Nothing to export", "Add some transactions first.");
            return;
        }

        static string Esc(string? s) => $"\"{(s ?? string.Empty).Replace("\"", "\"\"")}\"";

        var sb = new StringBuilder();
        sb.AppendLine("Date,Title,Category,Type,Amount,Note");
        foreach (var t in tx)
        {
            sb.Append(t.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).Append(',')
              .Append(Esc(t.Title)).Append(',')
              .Append(Esc(t.Category.Name)).Append(',')
              .Append(t.IsIncome ? "Income" : "Expense").Append(',')
              .Append(t.Amount.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
              .AppendLine(Esc(t.Note));
        }

        var path = Path.Combine(FileSystem.CacheDirectory, $"lumen-export-{DateTime.Now:yyyyMMdd-HHmm}.csv");
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);

        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export transactions",
                File = new ShareFile(path, "text/csv")
            });
        }
        catch
        {
            await Dialogs.Alert("Exported", $"Saved {tx.Count} transactions to:\n{path}");
        }
    }

    [RelayCommand]
    private async Task ReloadDemo()
    {
        if (!await Dialogs.Confirm("Reload demo data", "This replaces all your data with fresh sample data.", "Reload")) return;
        await _db.ResetAsync(reseed: true);
        Haptics.LongPress();
        await LoadAsync();
        await Dialogs.Alert("Done ✨", "Demo data has been regenerated.");
    }

    [RelayCommand]
    private async Task EraseAll()
    {
        if (!await Dialogs.Confirm("Erase everything?", "All transactions, budgets and goals will be permanently deleted.", "Erase")) return;
        await _db.ResetAsync(reseed: false);
        Haptics.LongPress();
        await LoadAsync();
    }

    [RelayCommand]
    private void ReplayOnboarding()
    {
        _settings.HasOnboarded = false;
        if (Application.Current?.Windows.Count > 0)
            Application.Current.Windows[0].Page = new OnboardingPage();
    }
}
