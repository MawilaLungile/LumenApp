namespace Lumen.Views;

public partial class TransactionEditorPage : ContentPage
{
    private readonly TransactionEditorViewModel _vm;

    public TransactionEditorPage()
    {
        InitializeComponent();
        BindingContext = _vm = ServiceHelper.Get<TransactionEditorViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // New transaction: jump straight into the amount field.
        if (!_vm.CanDelete && string.IsNullOrEmpty(_vm.AmountText))
        {
            await Task.Delay(350);
            AmountEntry.Focus();
        }
    }

    private async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not View view || view.BindingContext is not CategoryChoice choice) return;

        Haptics.Click();
        _vm.Select(choice);
        await view.ScaleTo(0.9, 70, Easing.CubicOut);
        await view.ScaleTo(1, 160, Easing.SpringOut);
    }
}
