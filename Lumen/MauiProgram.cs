using Microsoft.Extensions.Logging;

namespace Lumen;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ---- Services (singletons) ----
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<SettingsService>();

        // ---- ViewModels ----
        builder.Services.AddTransient<OnboardingViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<TransactionsViewModel>();
        builder.Services.AddTransient<TransactionEditorViewModel>();
        builder.Services.AddTransient<BudgetsViewModel>();
        builder.Services.AddTransient<GoalsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        ConfigureHandlers();

        return builder.Build();
    }

    /// <summary>
    /// Platform handler customisation: borderless entries for a clean, card-based look.
    /// </summary>
    private static void ConfigureHandlers()
    {
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Borderless", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
#endif
        });
    }
}
