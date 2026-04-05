using Microsoft.Maui.Controls;

namespace FlyACat;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // 关键：必须用 NavigationPage 包裹 MainPage
        return new Window(new NavigationPage(new MainPage()));
    }
}