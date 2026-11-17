namespace Lumen;

public partial class AppShell : Shell
{
    // Routes are registered once per process (AppShell can be re-created after onboarding).
    static AppShell()
    {
        Routing.RegisterRoute(Routes.Editor, typeof(TransactionEditorPage));
    }

    public AppShell()
    {
        InitializeComponent();
    }
}

public static class Routes
{
    public const string Editor = "editor";
    public const string Activity = "//activity";
}
