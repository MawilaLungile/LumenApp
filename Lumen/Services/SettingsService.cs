namespace Lumen.Services;

/// <summary>Strongly-typed wrapper over MAUI Preferences.</summary>
public class SettingsService
{
    private const string UserNameKey = "user_name";
    private const string CurrencyKey = "currency";
    private const string ThemeKey = "theme";
    private const string OnboardedKey = "has_onboarded";
    private const string FirstLaunchKey = "first_launch";

    public string UserName
    {
        get => Preferences.Default.Get(UserNameKey, "Friend");
        set => Preferences.Default.Set(UserNameKey, value);
    }

    public string Currency
    {
        get => Preferences.Default.Get(CurrencyKey, "R");
        set => Preferences.Default.Set(CurrencyKey, value);
    }

    /// <summary>0 = follow system, 1 = light, 2 = dark.</summary>
    public int Theme
    {
        get => Preferences.Default.Get(ThemeKey, 0);
        set => Preferences.Default.Set(ThemeKey, value);
    }

    public bool HasOnboarded
    {
        get => Preferences.Default.Get(OnboardedKey, false);
        set => Preferences.Default.Set(OnboardedKey, value);
    }

    public DateTime FirstLaunch
    {
        get
        {
            var stored = Preferences.Default.Get(FirstLaunchKey, DateTime.MinValue);
            if (stored == DateTime.MinValue)
            {
                stored = DateTime.Now;
                Preferences.Default.Set(FirstLaunchKey, stored);
            }
            return stored;
        }
    }

    public void ApplyTheme()
    {
        if (Application.Current is null) return;

        Application.Current.UserAppTheme = Theme switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    public static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
