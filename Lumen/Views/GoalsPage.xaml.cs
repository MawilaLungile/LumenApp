namespace Lumen.Views;

public partial class GoalsPage : ContentPage
{
    private readonly GoalsViewModel _vm;

    public GoalsPage()
    {
        InitializeComponent();
        BindingContext = _vm = ServiceHelper.Get<GoalsViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnGoalTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not View view || view.BindingContext is not SavingsGoal goal) return;

        await view.ScaleTo(0.95, 70, Easing.CubicOut);
        await view.ScaleTo(1, 140, Easing.SpringOut);
        await _vm.OpenAsync(goal);
    }
}
