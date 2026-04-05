using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace FlyACat;

public partial class MainPage : ContentPage
{
    // 定义存储的“钥匙”（Key），用于存取数据
    private const string AvatarKey = "UserAvatarPath";
    private const string UserIdKey = "SavedUserId";

    // 自定义头像图片列表
    private string[] _fakeGalleries = {
        "dotnet_bot.png",
        "avatar_cat_cool.png",
        "avatar_cat_cute.png",
        "avatar_cat_ninja.png"
    };

    private int _currentIndex = 0;

    public MainPage()
    {
        InitializeComponent();

        // ⭐ 核心功能：程序启动时加载上次保存的数据
        LoadUserData();
    }

    /// <summary>
    /// 从本地存储加载头像和 ID
    /// </summary>
    private void LoadUserData()
    {
        // 1. 读取保存的 ID，如果没有存过，默认显示 "Player1"
        string savedId = Preferences.Default.Get(UserIdKey, "Player1");
        UserIdEntry.Text = savedId;

        // 2. 读取保存的头像路径，如果没有存过，默认显示 "dotnet_bot.png"
        string savedAvatar = Preferences.Default.Get(AvatarKey, "dotnet_bot.png");
        UserAvatar.Source = savedAvatar;

        // 同步当前的索引，防止点击 Next 时跳回第一张
        _currentIndex = Array.IndexOf(_fakeGalleries, savedAvatar);
        if (_currentIndex < 0) _currentIndex = 0;
    }

    /// <summary>
    /// 当用户修改 ID 输入框时，实时保存
    /// </summary>
    private void OnIdChanged(object sender, TextChangedEventArgs e)
    {
        // ⭐ 实时保存 ID 到本地
        Preferences.Default.Set(UserIdKey, e.NewTextValue);
    }

    private void OnVirtualCameraClicked(object sender, EventArgs e)
    {
        if (!CameraOverlay.IsVisible)
        {
            CameraOverlay.IsVisible = true;
            BtnCapture.IsVisible = true;
            BtnVirtualPhoto.Text = "🔄 Next";
            UpdatePreview();
        }
        else
        {
            _currentIndex = (_currentIndex + 1) % _fakeGalleries.Length;
            UpdatePreview();
        }
    }

    private void UpdatePreview()
    {
        PreviewImage.Source = _fakeGalleries[_currentIndex];
    }

    private async void OnCaptureClicked(object sender, EventArgs e)
    {
        // 模拟快门
        await CameraOverlay.FadeTo(0.2, 100);
        await CameraOverlay.FadeTo(1.0, 100);

        string selectedAvatar = _fakeGalleries[_currentIndex];

        // 1. 更新 UI 显示
        UserAvatar.Source = selectedAvatar;

        // ⭐ 2. 核心功能：将选择的头像文件名保存到本地
        Preferences.Default.Set(AvatarKey, selectedAvatar);

        await DisplayAlert("Saved", "Profile updated and saved!", "OK");

        CameraOverlay.IsVisible = false;
        BtnCapture.IsVisible = false;
        BtnVirtualPhoto.Text = "📷 Photo";
       
    }

    // --- 页面跳转逻辑 ---
    private async void OnStartGameClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new GamePage());
    }

    private async void OnSelectLevelClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LevelSelectPage());
    }
}