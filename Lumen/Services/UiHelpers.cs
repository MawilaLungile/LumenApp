namespace Lumen.Services;

/// <summary>Resolves services for pages that are created by Shell/XAML (parameterless constructors).</summary>
public static class ServiceHelper
{
    public static T Get<T>() where T : notnull =>
        IPlatformApplication.Current!.Services.GetRequiredService<T>();
}

/// <summary>Thin wrappers over MAUI dialogs so view models stay tidy.</summary>
public static class Dialogs
{
    private static Page Current => Shell.Current ?? Application.Current!.Windows[0].Page!;

    public static Task Alert(string title, string message) =>
        Current.DisplayAlert(title, message, "OK");

    public static Task<bool> Confirm(string title, string message, string accept = "Yes", string cancel = "Cancel") =>
        Current.DisplayAlert(title, message, accept, cancel);

    public static Task<string> Sheet(string title, string? destruction, params string[] buttons) =>
        Current.DisplayActionSheet(title, "Cancel", destruction, buttons);

    public static Task<string> Prompt(string title, string message, string placeholder, int maxLength = 40, string initial = "") =>
        Current.DisplayPromptAsync(title, message, "Next", "Cancel", placeholder: placeholder, maxLength: maxLength, initialValue: initial);

    public static async Task<double?> PromptAmount(string title, string message, double? initial = null)
    {
        var result = await Current.DisplayPromptAsync(
            title, message, "Save", "Cancel",
            placeholder: "0.00",
            keyboard: Keyboard.Numeric,
            initialValue: initial?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty);

        if (string.IsNullOrWhiteSpace(result)) return null;

        if (Money.TryParse(result, out var value) && value > 0) return value;

        await Alert("Invalid amount", "Please enter a number greater than zero.");
        return null;
    }
}

public static class Haptics
{
    public static void Click()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch { /* Not supported on this device (e.g. desktop) */ }
    }

    public static void LongPress()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); }
        catch { /* Not supported */ }
    }
}
