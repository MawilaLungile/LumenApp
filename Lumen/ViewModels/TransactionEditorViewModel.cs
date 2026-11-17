namespace Lumen.ViewModels;

/// <summary>A selectable category tile in the editor.</summary>
public partial class CategoryChoice : ObservableObject
{
    public CategoryChoice(Category category)
    {
        Category = category;
    }

    public Category Category { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TileColor))]
    [NotifyPropertyChangedFor(nameof(TileStroke))]
    private bool isSelected;

    public Color TileColor => IsSelected ? Category.SoftColor : Colors.Transparent;

    public Brush TileStroke => new SolidColorBrush(IsSelected ? Category.Color : Colors.Transparent);
}

/// <summary>Create / edit a transaction. Navigated to with "editor" or "editor?id=42".</summary>
public partial class TransactionEditorViewModel : ObservableObject, IQueryAttributable
{
    private readonly DatabaseService _db;
    private TransactionEntry? _existing;
    private CategoryChoice? _selected;

    public TransactionEditorViewModel(DatabaseService db)
    {
        _db = db;
        BuildCategories(null);
    }

    [ObservableProperty] private string pageTitle = "New transaction";
    [ObservableProperty] private string saveText = "Save transaction";
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string amountText = string.Empty;
    [ObservableProperty] private string note = string.Empty;
    [ObservableProperty] private DateTime? date = DateTime.Today;
    [ObservableProperty] private TimeSpan? time = DateTime.Now.TimeOfDay;
    [ObservableProperty] private bool canDelete;
    [ObservableProperty] private List<CategoryChoice> categoryChoices = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExpense))]
    private bool isIncome;

    public bool IsExpense => !IsIncome;

    public string CurrencySymbol => Money.Symbol;

    partial void OnIsIncomeChanged(bool value) => BuildCategories(null);

    private void BuildCategories(string? selectId)
    {
        var source = IsIncome ? Categories.Income : Categories.Expenses;
        var choices = source.Select(c => new CategoryChoice(c)).ToList();
        CategoryChoices = choices;

        _selected = null;
        var toSelect = choices.FirstOrDefault(c => c.Category.Id == selectId) ?? choices[0];
        Select(toSelect);
    }

    public void Select(CategoryChoice choice)
    {
        if (_selected is not null) _selected.IsSelected = false;
        _selected = choice;
        choice.IsSelected = true;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var raw) && int.TryParse(raw?.ToString(), out var id) && id > 0)
            _ = LoadAsync(id);
    }

    private async Task LoadAsync(int id)
    {
        _existing = await _db.GetTransactionAsync(id);
        if (_existing is null) return;

        PageTitle = "Edit transaction";
        SaveText = "Update transaction";
        CanDelete = true;

        Title = _existing.Title;
        AmountText = _existing.Amount.ToString("0.##", CultureInfo.InvariantCulture);
        Note = _existing.Note ?? string.Empty;
        IsIncome = _existing.IsIncome;
        BuildCategories(_existing.CategoryId);
        Date = _existing.Date.Date;
        Time = _existing.Date.TimeOfDay;
    }

    [RelayCommand]
    private void SetType(string type)
    {
        Haptics.Click();
        IsIncome = type == "income";
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!Money.TryParse(AmountText, out var amount) || amount <= 0)
        {
            await Dialogs.Alert("Amount needed", "Please enter an amount greater than zero.");
            return;
        }

        var category = (_selected ?? CategoryChoices[0]).Category;

        var entry = _existing ?? new TransactionEntry();
        entry.Title = string.IsNullOrWhiteSpace(Title) ? category.Name : Title.Trim();
        entry.Amount = Math.Round(amount, 2);
        entry.IsIncome = IsIncome;
        entry.CategoryId = category.Id;
        entry.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
        entry.Date = (Date ?? DateTime.Today).Date + (Time ?? TimeSpan.Zero);

        await _db.SaveTransactionAsync(entry);
        Haptics.Click();
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (_existing is null) return;

        var ok = await Dialogs.Confirm("Delete transaction", $"Delete \"{_existing.Title}\"? This can't be undone.", "Delete");
        if (!ok) return;

        await _db.DeleteTransactionAsync(_existing);
        Haptics.LongPress();
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task Close() => Shell.Current.GoToAsync("..");
}
