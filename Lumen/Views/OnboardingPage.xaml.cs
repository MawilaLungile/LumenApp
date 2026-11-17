namespace Lumen.Views;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.Get<OnboardingViewModel>();
    }
}
