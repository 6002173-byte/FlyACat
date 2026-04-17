
using Microsoft.Maui.Controls;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace FlyACat;

public partial class GamePage : ContentPage
{
    private const int Rows = 12;
    private const int Cols = 12;
    private int _lives = 3;
    private int _catsRemaining=5;
    private bool _isAnimating = false;
    private int[,] _gridMap;
    private List<CatSegment> _allCats = new();

    public class CatSegment
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsHead { get; set; }
        public string ColorKey { get; set; }
        public int BodyIndex { get; set; }
        public Button ButtonRef { get; set; }
        public View ContainerRef { get; set; }
    }

    public GamePage()
    {
        InitializeComponent();
        InitGame();
    }

    private void InitGame()
    {
        _lives = 3;
        _isAnimating = false;
        _catsRemaining = 5;
        _allCats.Clear();
        _gridMap = new int[Rows, Cols];

        GameOverFrame.IsVisible = false;
        NextLevelButton.IsVisible = false;
        RemainingLabel.Text = _catsRemaining.ToString();
        UpdateStarsUI();

        // Initialize the grid
        GameGrid.ColumnDefinitions.Clear();
        GameGrid.RowDefinitions.Clear();
        for (int i = 0; i < Cols; i++) GameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (int i = 0; i < Rows; i++) GameGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

        // Create the 5 cats for the first level
        CreateCat("Teal", (1, 1), (2, 1), (3, 1),(4,1),(5,1),(6,1)); 
        CreateCat("Pink", (9, 10), (8, 10), (7, 10)); 
        CreateCat("Blue", (1, 5), (1, 4), (1, 3)); 
        CreateCat("Yellow", (10, 5), (10, 6), (10, 7)); 
        CreateCat("Purple", (5, 5), (5, 6), (5, 7)); 

        RenderGrid();
    }

    private void CreateCat(string color, params (int r, int c)[] body)
    {
        for (int i = 0; i < body.Length; i++)
        {
            var seg = new CatSegment
            { Row = body[i].r, Col = body[i].c, IsHead = (i == 0), BodyIndex = i, ColorKey = color };
            _allCats.Add(seg);
            _gridMap[seg.Row, seg.Col] = 1;
        }
    }

    private void RenderGrid()
    {
       
              
        GameGrid.Children.Clear();
        foreach (var seg in _allCats.OrderBy(s => s.IsHead))
        {
            var container = new AbsoluteLayout();
            var btn = new Button
            {
                Text = seg.IsHead ? GetCatIcon(seg) : "♦",
                BackgroundColor = GetColor(seg.ColorKey),
                TextColor = Colors.White,
                Padding = 0,
                FontSize = 14,
                CornerRadius = 8,
                ZIndex = seg.IsHead ? 10 : 0
            };

            if (seg.IsHead) btn.Clicked += OnCatHeadClicked;
            else btn.IsEnabled = false;

            container.Children.Add(btn);
            GameGrid.Add(container, seg.Col, seg.Row);
            seg.ButtonRef = btn;
            seg.ContainerRef = container;
        }
    }

    private string GetCatIcon(CatSegment head)
    {
        var neck = _allCats.FirstOrDefault(s => s.ColorKey == head.ColorKey && s.BodyIndex == 1);
        if (neck == null) return "😺";
        int dr = head.Row - neck.Row;
        int dc = head.Col - neck.Col;
        if (dr < 0) return "😺⬆️";
        if (dr > 0) return "😺⬇️";
        if (dc < 0) return "⬅️😺";
        return "😺➡️";
    }

    private async void OnCatHeadClicked(object sender, EventArgs e)
    {
        if (_isAnimating || _lives <= 0) return;
        var head = _allCats.FirstOrDefault(s => s.ButtonRef == (Button)sender);
        var body = _allCats.Where(s => s.ColorKey == head.ColorKey).OrderBy(s => s.BodyIndex).ToList();

        int dr = Math.Sign(head.Row - body[1].Row);
        int dc = Math.Sign(head.Col - body[1].Col);

        if (!IsPathClear(head, dr, dc, body))
        {
            await HandleCrash(head);
            return;
        }

        await FlyAway(body, dr, dc);
    }

    private bool IsPathClear(CatSegment head, int dr, int dc, List<CatSegment> body)
    {
        int r = head.Row + dr;
        int c = head.Col + dc;
        while (r >= 0 && r < Rows && c >= 0 && c < Cols)
        {
            if (!body.Any(b => b.Row == r && b.Col == c) && _gridMap[r, c] == 1) return false;
            r += dr; c += dc;
        }
        return true;
    }

    private async Task FlyAway(List<CatSegment> body, int dr, int dc)
    {
        _isAnimating = true;
        var tasks = body.Select(s => Task.WhenAll(
            s.ContainerRef.TranslateTo(dc * 800, dr * 800, 400, Easing.CubicIn),
            s.ContainerRef.FadeTo(0, 400)
        ));
        await Task.WhenAll(tasks);

        foreach (var s in body)
        {
            _gridMap[s.Row, s.Col] = 0;
            _allCats.Remove(s);
            GameGrid.Children.Remove(s.ContainerRef);
        }

        _catsRemaining--;
        RemainingLabel.Text = _catsRemaining.ToString();
        if (_catsRemaining <= 0) ShowGameOver(true);
        _isAnimating = false;
    }

    private async Task HandleCrash(CatSegment head)//Collision effect
    {
        _isAnimating = true;
        head.ButtonRef.Text = "💥";// Change the cat's head into an explosion
        for (int i = 0; i < 2; i++) { await head.ButtonRef.TranslateTo(8, 0, 50); await head.ButtonRef.TranslateTo(-8, 0, 50); }// Move left and right twice to imitate the vibration.
        await head.ButtonRef.TranslateTo(0, 0, 50);//resume seat
        head.ButtonRef.Text = GetCatIcon(head);//Restore the cat's head
        _lives--;
        UpdateStarsUI();
        if (_lives <= 0) ShowGameOver(false);
        _isAnimating = false;
    }

    private void UpdateStarsUI()
    {
        Star1.Text = _lives >= 1 ? "❤️" : "🖤";
        Star2.Text = _lives >= 2 ? "❤️" : "🖤";
        Star3.Text = _lives >= 3 ? "❤️" : "🖤";
    }

    private void ShowGameOver(bool win)
    {
        GameOverFrame.IsVisible =
    true
    ;
        GameOverText.Text = win ?
    "🎉 SUCCESS!" : "💥 FAILED"
    ;
        GameOverSubText.Text = win ?
    "You saved all cats!" : "Try to avoid collisions!"
    ;
        NextLevelButton.IsVisible = win;

        // Trigger notification upon completing the first level
        if
     (win)
        {
            SendDelayedNotification();
        }
    }
    private void SendDelayedNotification()
    {
        var request = new NotificationRequest
        {
            NotificationId = 1000, // The first level uses 1000.
            Title = "FLYACAT - LEVEL1",
            Description = "The cat in the first level has taken off safely! Come back and challenge the more difficult levels!！",
            Schedule = new NotificationRequestSchedule
            {
                // Push will be triggered after 10 seconds.
                NotifyTime = DateTime.Now.AddSeconds(10)
            }
        };

        LocalNotificationCenter.Current.Show(request);
    }

    private Color GetColor(string key) => key switch
    {
        "Teal" => Colors.Teal,
        "Pink" => Colors.DeepPink,
        "Blue" => Colors.DodgerBlue,
        "Yellow" => Colors.Orange,
        "Purple" => Colors.MediumPurple,
        _ => Colors.Gray
    };

    private async void OnNextLevelClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Level2Page());
    private void OnRestartClicked(object sender, EventArgs e) => InitGame();
    private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}