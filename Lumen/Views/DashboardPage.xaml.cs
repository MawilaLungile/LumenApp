namespace Lumen.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;
    private bool _hasAnimatedIn;

    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = _vm = ServiceHelper.Get<DashboardViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var sections = new VisualElement[] { Header, BalanceCard, InsightCard, WeekCard, CategoryCard, RecentSection };
        if (!_hasAnimatedIn)
        {
            foreach (var s in sections)
            {
                s.Opacity = 0;
                s.TranslationY = 28;
            }
        }

        await _vm.LoadAsync();

        if (!_hasAnimatedIn)
        {
            _hasAnimatedIn = true;
            await StaggerInAsync(sections);
            await PulseFabAsync();
        }
    }

    /// <summary>Cascading fade + slide entrance for each dashboard section.</summary>
    private static async Task StaggerInAsync(IEnumerable<VisualElement> elements)
    {
        foreach (var element in elements)
        {
            _ = element.FadeTo(1, 420, Easing.CubicOut);
            _ = element.TranslateTo(0, 0, 520, Easing.CubicOut);
            await Task.Delay(70);
        }
    }

    private async Task PulseFabAsync()
    {
        await Fab.ScaleTo(1.12, 160, Easing.CubicOut);
        await Fab.ScaleTo(1.0, 220, Easing.SpringOut);
    }
}
