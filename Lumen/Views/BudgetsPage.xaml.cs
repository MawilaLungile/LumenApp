namespace Lumen.Views;

public partial class BudgetsPage : ContentPage
{
    private readonly BudgetsViewModel _vm;

    public BudgetsPage()
    {
        InitializeComponent();
        BindingContext = _vm = ServiceHelper.Get<BudgetsViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnBudgetTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is BudgetItem item)
            await _vm.EditAsync(item);
    }
}
