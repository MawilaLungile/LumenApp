namespace Lumen;

public partial class App : Application
{
    private readonly SettingsService _settings;

    public App(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;

        Money.Symbol = settings.Currency;
        settings.ApplyTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Page root = _settings.HasOnboarded ? new AppShell() : new OnboardingPage();
        var window = new Window(root) { Title = "Lumen" };

        // On desktop, open in a phone-shaped window so the mobile design shines.
        if (DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
        {
            window.Width = 440;
            window.Height = 900;
            window.MinimumWidth = 380;
            window.MinimumHeight = 640;
        }

        return window;
    }
}
