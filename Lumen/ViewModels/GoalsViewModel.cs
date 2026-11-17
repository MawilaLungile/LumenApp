namespace Lumen.ViewModels;

public partial class GoalsViewModel : ObservableObject
{
    private static readonly string[] Icons = { "🛟", "💻", "🏖️", "🚗", "🏠", "🎓", "💍", "🎮", "📱", "✈️", "🐶", "⭐" };
    private static readonly string[] Palette = { "#7C5CFF", "#00B4D8", "#FFB547", "#F472B6", "#22C55E", "#FF6B6B", "#60A5FA" };

    private readonly DatabaseService _db;

    public GoalsViewModel(DatabaseService db)
    {
        _db = db;
    }

    [ObservableProperty] private List<SavingsGoal> goals = new();
    [ObservableProperty] private string totalSavedText = string.Empty;
    [ObservableProperty] private string totalTargetText = string.Empty;
    [ObservableProperty] private double overallProgress;
    [ObservableProperty] private string overallPercentText = "0%";
    [ObservableProperty] private string completedText = string.Empty;
    [ObservableProperty] private bool isRefreshing;

    public async Task LoadAsync()
    {
        Goals = await _db.GetGoalsAsync();

        var saved = Goals.Sum(g => g.Saved);
        var target = Goals.Sum(g => g.Target);

        TotalSavedText = Money.Format(saved);
        TotalTargetText = $"of {Money.Format(target)} across {Goals.Count} goals";
        OverallProgress = target > 0 ? Math.Clamp(saved / target, 0, 1) : 0;
        OverallPercentText = Money.Percent(OverallProgress);

        var done = Goals.Count(g => g.IsComplete);
        CompletedText = done == 0 ? "Keep going — you've got this 💪" : $"🏆 {done} goal{(done == 1 ? "" : "s")} completed";
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

        var name = await Dialogs.Prompt("New savings goal", "What are you saving for?", "e.g. Dream vacation");
        if (string.IsNullOrWhiteSpace(name)) return;

        var icon = await Dialogs.Sheet("Pick an icon", null, Icons);
        if (string.IsNullOrEmpty(icon) || !Icons.Contains(icon)) icon = "⭐";

        var target = await Dialogs.PromptAmount("Target amount", $"How much do you need in {Money.Symbol}?");
        if (target is null) return;

        var timeframe = await Dialogs.Sheet("When do you want to reach it?", null, "3 months", "6 months", "12 months", "24 months");
        var months = int.TryParse(timeframe?.Split(' ')[0], out var m) ? m : 6;

        var goal = new SavingsGoal
        {
            Name = name.Trim(),
            Icon = icon,
            Target = target.Value,
            Saved = 0,
            ColorHex = Palette[Goals.Count % Palette.Length],
            Deadline = DateTime.Today.AddMonths(months),
            CreatedAt = DateTime.Now
        };

        await _db.SaveGoalAsync(goal);
        await LoadAsync();
    }

    public async Task OpenAsync(SavingsGoal goal)
    {
        Haptics.Click();
        const string add = "➕  Add money";
        const string withdraw = "➖  Withdraw";

        var action = await Dialogs.Sheet($"{goal.Icon} {goal.Name} · {goal.PercentText}", "Delete goal", add, withdraw);

        switch (action)
        {
            case add:
            {
                var amount = await Dialogs.PromptAmount("Add to goal", $"How much are you adding to {goal.Name}?");
                if (amount is null) return;

                var wasComplete = goal.IsComplete;
                goal.Saved += amount.Value;
                await _db.SaveGoalAsync(goal);
                await LoadAsync();

                if (!wasComplete && goal.IsComplete)
                {
                    Haptics.LongPress();
                    await Dialogs.Alert("🎉 Goal reached!", $"Amazing — you've fully funded \"{goal.Name}\". Time to celebrate!");
                }
                break;
            }
            case withdraw:
            {
                var amount = await Dialogs.PromptAmount("Withdraw from goal", $"Currently saved: {goal.SavedText}");
                if (amount is null) return;

                goal.Saved = Math.Max(0, goal.Saved - amount.Value);
                await _db.SaveGoalAsync(goal);
                await LoadAsync();
                break;
            }
            case "Delete goal":
            {
                if (!await Dialogs.Confirm("Delete goal", $"Delete \"{goal.Name}\"?", "Delete")) return;
                await _db.DeleteGoalAsync(goal);
                await LoadAsync();
                break;
            }
        }
    }
}
