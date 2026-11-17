namespace Lumen.Views.Components;

/// <summary>Reusable transaction card. Tapping it opens the editor.</summary>
public partial class TransactionRow : ContentView
{
    public TransactionRow()
    {
        InitializeComponent();
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is not TransactionEntry entry) return;

        await Card.ScaleTo(0.97, 70, Easing.CubicOut);
        await Card.ScaleTo(1, 90, Easing.CubicOut);
        await Shell.Current.GoToAsync($"{Routes.Editor}?id={entry.Id}");
    }
}
