namespace Lumen.ViewModels;

public sealed record OnboardingSlide(string Emoji, string Title, string Subtitle);

public partial class OnboardingViewModel : ObservableObject
{
    private readonly SettingsService _settings;

    public OnboardingViewModel(SettingsService settings)
    {
        _settings = settings;
    }

    public List<OnboardingSlide> Slides { get; } = new()
    {
        new("✨", "Welcome to Lumen", "A beautiful, private home for your money. Everything stays on your device."),
        new("📊", "See where it goes", "Animated charts and smart weekly insights turn your spending into clear, simple stories."),
        new("🎯", "Budgets that guide you", "Set monthly limits per category and get a gentle nudge before you overspend."),
        new("🚀", "Grow your goals", "Save for the things that matter, track progress rings and celebrate every milestone."),
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLastSlide))]
    [NotifyPropertyChangedFor(nameof(IsNotLastSlide))]
    [NotifyPropertyChangedFor(nameof(NextText))]
    private int position;

    [ObservableProperty]
    private string name = string.Empty;

    public bool IsLastSlide => Position >= Slides.Count - 1;

    public bool IsNotLastSlide => !IsLastSlide;

    public string NextText => IsLastSlide ? "Get started" : "Continue";

    [RelayCommand]
    private void Next()
    {
        Haptics.Click();
        if (!IsLastSlide)
        {
            Position++;
            return;
        }

        _settings.UserName = string.IsNullOrWhiteSpace(Name) ? "Friend" : Name.Trim();
        _settings.HasOnboarded = true;

        if (Application.Current?.Windows.Count > 0)
            Application.Current.Windows[0].Page = new AppShell();
    }

    [RelayCommand]
    private void Skip() => Position = Slides.Count - 1;
}
