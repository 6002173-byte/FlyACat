using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace FlyACat;

public partial class MainPage : ContentPage
{
    // Define the keys for storing names and avatars
    private const string AvatarKey = "UserAvatarPath";
    private const string UserIdKey = "SavedUserId";

    // List of profile pictures
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

        
        LoadUserData();
    }

    // Load the avatar and ID from local storage
    private void LoadUserData()
    {
        // Read the saved ID. If no ID has been saved, display "Player1" by default.
        string savedId = Preferences.Default.Get(UserIdKey, "Player1");
        UserIdEntry.Text = savedId;

        // Read the saved avatar path. If no avatar has been saved before, display "dotnet_bot.png" by default.
        string savedAvatar = Preferences.Default.Get(AvatarKey, "dotnet_bot.png");
        UserAvatar.Source = savedAvatar;

        // Synchronize the current index to prevent jumping back to the first image when clicking "Next"
        _currentIndex = Array.IndexOf(_fakeGalleries, savedAvatar);// If no image is found, return -1 as the default avatar.
        if (_currentIndex < 0) _currentIndex = 0;
    }


    // When the user modifies the input field for ID, the information is saved in real time.
    private void OnIdChanged(object sender, TextChangedEventArgs e)
    {
        // Save the ID in real time to the local storage
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
        // Simulation Shutter
        await CameraOverlay.FadeTo(0.2, 100);
        await CameraOverlay.FadeTo(1.0, 100);

        string selectedAvatar = _fakeGalleries[_currentIndex];

        // Update UI display
        UserAvatar.Source = selectedAvatar;

        // Save the selected avatar file name to the local storage
        Preferences.Default.Set(AvatarKey, selectedAvatar);

        await DisplayAlert("Saved", "Profile updated and saved!", "OK");

        CameraOverlay.IsVisible = false;
        BtnCapture.IsVisible = false;
        BtnVirtualPhoto.Text = "📷 Photo";
       
    }

    // Page transition logic
    private async void OnStartGameClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new GamePage());
    }

    private async void OnSelectLevelClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LevelSelectPage());
    }
}