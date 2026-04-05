namespace FlyACat;

public partial class LevelSelectPage : ContentPage
{
    public LevelSelectPage() { InitializeComponent(); }

    private async void OnLevel1Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new GamePage());
    private async void OnLevel2Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new Level2Page());
    private async void OnLevel3Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new Level3Page());
    private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}