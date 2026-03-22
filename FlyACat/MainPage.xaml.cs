using Microsoft.Maui.Storage;

namespace FlyACat;

public partial class MainPage : ContentPage
{
    // 定义存储文件名常量 
    private const string PreferenceKeyId = "user_id";

    public MainPage()
    {
        InitializeComponent();
        // 页面加载完成后，加载保存的数据
        Loaded += OnPageLoaded;
    }

    // === 应用启动时，恢复用户上次的状态 ===
    private async void OnPageLoaded(object sender, EventArgs e)
    {
        // 防止多次触发
        Loaded -= OnPageLoaded;
        await LoadProfileAsync();
    }

    // === 加载数据  ===
    private async Task LoadProfileAsync()
    {
        try
        {
            // 加载 ID
            string savedId = Preferences.Get(PreferenceKeyId, "Player1");
            UserIdEntry.Text = savedId;

           
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading profile: {ex.Message}");
        }
    }

    // === 保存数据  ===
    private async Task SaveProfileAsync()
    {
        try
        {
            // 保存 ID
            string currentId = UserIdEntry.Text ?? "Player1"; //如果是空的就改成 "Player1"。
            Preferences.Set(PreferenceKeyId, currentId);

            
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving profile: {ex.Message}");
            await DisplayAlert("Error", $"Failed to save profile: {ex.Message}", "OK");
        }
    }

    // === ID 改变时自动保存 ===
    private async void OnIdChanged(object sender, TextChangedEventArgs e)
    {
        await SaveProfileAsync();//如果有改变就保存
    }

    // === 从相册选择  ===
    private async void OnPickFromGalleryClicked(object sender, EventArgs e)
    {
       
        
    }

    // === 拍照  ===
    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
       
    }

    private async void OnStartGameClicked(object sender, EventArgs e)
    {
        string userId = UserIdEntry.Text ?? "Unknown";
        await DisplayAlert("Game Status", $"Hello {userId}!\nReady to fly?", "Let's Go");
        // 可以在这里跳转到游戏主界面
        // await Navigation.PushAsync(new GamePage());
    }

    private async void OnSelectLevelClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LevelSelectPage());
    }
}