namespace FlyACat;

public partial class LevelSelectPage : ContentPage
{
    public LevelSelectPage()
    {
        InitializeComponent();
    }

    private async void OnLevel1Clicked(object sender, EventArgs e)
    {
        await DisplayAlert("Level 1", "You selected Level 1!\nLoading...", "Play");
        // 这里以后可以跳转到具体的游戏页面
    }

    private async void OnLevel2Clicked(object sender, EventArgs e)
    {
        await DisplayAlert("Level 2", "You selected Level 2!\nLoading...", "Play");
    }

    private async void OnLevel3Clicked(object sender, EventArgs e)
    {
        await DisplayAlert("Level 3", "You selected level 3!\nLoading...", "Play");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        // 返回到上一个页面 (MainPage)
        await Navigation.PopAsync();
    }
}