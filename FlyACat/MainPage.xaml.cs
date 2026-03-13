using Microsoft.Maui.Storage;

namespace FlyACat;

public partial class MainPage : ContentPage
{
    // 定义存储文件名常量
    private const string AvatarFileName = "user_avatar.png";
    private const string PreferenceKeyId = "user_id";
    private const string PreferenceKeyAvatarPath = "user_avatar_path";

    public MainPage()
    {
        InitializeComponent();
        // 页面加载完成后，加载保存的数据
        Loaded += OnPageLoaded;
    }

    // === 应用启动时，恢复用户上次的状态 ===
    private async void OnPageLoaded(object sender, EventArgs e)
    {
        // 移除事件监听，防止多次触发
        Loaded -= OnPageLoaded;
        await LoadProfileAsync();
    }

    // === 加载数据 ===
    private async Task LoadProfileAsync()
    {
        try
        {
            // 加载 ID
            string savedId = Preferences.Get(PreferenceKeyId, "Player1");
            UserIdEntry.Text = savedId;

            // 加载头像路径
            string savedPath = Preferences.Get(PreferenceKeyAvatarPath, string.Empty);

            if (!string.IsNullOrEmpty(savedPath))
            {
                if (File.Exists(savedPath))
                {
                    // 使用文件流加载图片到内存，避免文件被锁定
                    using var fileStream = File.OpenRead(savedPath);
                    using var memoryStream = new MemoryStream();

                    // 将文件内容拷贝到内存流
                    await fileStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0; // 重置指针到开头

                    // 将内存流赋值给 Image 控件
                    // 这里我们直接设置 Source 为从内存字节数组创建的流工厂
                    byte[] imageBytes = memoryStream.ToArray();
                    UserAvatar.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                }
                else
                {
                    // 文件不存在，清理无效的路径记录
                    Preferences.Remove(PreferenceKeyAvatarPath);
                    // 可选：重置为默认图片
                    UserAvatar.Source = "dotnet_bot.png";
                }
            }
            else
            {
                // 没有保存过路径，使用默认图片
                UserAvatar.Source = "dotnet_bot.png";
            }
        }
        catch (Exception ex)
        {
            // 记录错误并提示用户，但不阻断程序运行
            System.Diagnostics.Debug.WriteLine($"Error loading profile: {ex.Message}");

        }
    }

    // === 保存数据 ===
    private async Task SaveProfileAsync()
    {
        try
        {
            // 保存 ID
            string currentId = UserIdEntry.Text ?? "Player1";
            Preferences.Set(PreferenceKeyId, currentId);

            // 保存头像 
            if (UserAvatar.Source is StreamImageSource streamSource)
            {
                // 确定保存路径：应用数据目录 + 文件名
                string avatarPath = Path.Combine(FileSystem.AppDataDirectory, AvatarFileName);

                // 获取图像流并写入文件
                using var imageStream = await streamSource.Stream(CancellationToken.None);
                using var fileStream = File.Create(avatarPath);

                // 将图像流拷贝到文件
                await imageStream.CopyToAsync(fileStream);

                // 保存新路径到 Preferences
                Preferences.Set(PreferenceKeyAvatarPath, avatarPath);

                System.Diagnostics.Debug.WriteLine($"Profile saved successfully to: {avatarPath}");
            }
            else
            {
                // 如果是默认资源图片，不需要保存文件，只需清除路径记录
                // 防止引用了旧的已删除文件
                Preferences.Remove(PreferenceKeyAvatarPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving profile: {ex.Message}");
            // 保留用户可见的错误提示
            await DisplayAlert("Error", $"Failed to save profile: {ex.Message}", "OK");
        }
    }

    // === ID 改变时自动保存 ===
    private async void OnIdChanged(object sender, TextChangedEventArgs e)
    {
        // 直接保存
        await SaveProfileAsync();
    }

    // === 从相册选择 ===
    private async void OnPickFromGalleryClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select an avatar",
                FileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.image" } },
                        { DevicePlatform.Android, new[] { "image/*" } },
                        { DevicePlatform.WinUI, new[] { ".png", ".jpg", ".jpeg" } },
                    })
            });

            if (result == null) return; // 用户取消选择

            using var originalStream = await result.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await originalStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // 更新 UI：从内存字节数组创建图片源，确保流独立
            byte[] imageBytes = memoryStream.ToArray();
            UserAvatar.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));

            // 立即保存
            await SaveProfileAsync();
        }
        catch (Exception ex)
        {
            if (!ex.Message.Contains("canceled", StringComparison.OrdinalIgnoreCase))
                await DisplayAlert("Error", $"Failed to load image: {ex.Message}", "OK");
        }
    }

    // === 拍照 ===
    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        if (!MediaPicker.IsCaptureSupported)
        {
            await DisplayAlert("Alert", "Camera not supported on this device.", "OK");
            return;
        }

        try
        {
            var photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Take a profile photo"
            });

            if (photo == null) return; // 用户取消拍照

            using var originalStream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await originalStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // 更新 UI
            byte[] imageBytes = memoryStream.ToArray();
            UserAvatar.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));

            // 立即保存
            await SaveProfileAsync();
        }
        catch (Exception ex)
        {
            // 忽略用户取消操作，只报告真实错误
            if (!ex.Message.Contains("canceled", StringComparison.OrdinalIgnoreCase))
                await DisplayAlert("Error", $"Failed to take photo: {ex.Message}", "OK");
        }

    }

    private async void OnStartGameClicked(object sender, EventArgs e)
    {
        string userId = UserIdEntry.Text ?? "Unknown";
        await DisplayAlert("Game Status", $"Hello {userId}!\nReady to fly?", "Let's Go");
        // 未来可以在这里跳转到游戏主界面
        // await Navigation.PushAsync(new GamePage());
    }

    private async void OnSelectLevelClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LevelSelectPage());
    }
}