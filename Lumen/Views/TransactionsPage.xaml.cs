namespace Lumen.Views;

public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _vm;

    public TransactionsPage()
    {
        InitializeComponent();
        BindingContext = _vm = ServiceHelper.Get<TransactionsViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnDeleteInvoked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is TransactionEntry entry)
            await _vm.DeleteAsync(entry);
    }
}
