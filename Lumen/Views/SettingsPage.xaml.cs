namespace Lumen.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;

    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = _vm = ServiceHelper.Get<SettingsViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
