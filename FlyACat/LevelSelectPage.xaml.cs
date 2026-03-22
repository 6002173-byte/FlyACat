namespace FlyACat;

public partial class LevelSelectPage : ContentPage
{
    public LevelSelectPage()
    {
        InitializeComponent();
    }

    // 关卡 1 点击
    private async void OnLevel1Clicked(object sender, EventArgs e)
    {
        // 跳转到游戏页面
        // 未来可以通过构造函数传递参数 (如 new GamePage(1)) 来区分不同地图
        await Navigation.PushAsync(new GamePage());
    }

    // === 关卡 2 
    private async void OnLevel2Clicked(object sender, EventArgs e)
    {
       
        await Navigation.PushAsync(new GamePage());
    }

    // === 关卡 3 
    private async void OnLevel3Clicked(object sender, EventArgs e)
    {
        // 暂时跳转到 GamePage
        await Navigation.PushAsync(new GamePage());
    }

    // === 返回按钮点击事件 ===
    private async void OnBackClicked(object sender, EventArgs e)
    {
        // 弹出当前页面，返回到 MainPage
        await Navigation.PopAsync();
    }
}