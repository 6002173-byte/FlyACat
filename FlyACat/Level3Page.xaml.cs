using Microsoft.Maui.Controls;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlyACat;

public partial class Level3Page : ContentPage
{
    private const int Rows = 12;
    private const int Cols = 12;
    private const int TotalCatsToClear = 12;

    private int _lives = 3;
    private int _catsRemaining = 0;
    private bool _isAnimating = false;
    private int[,] _gridMap;
    private List<CatSegment> _allCats = new List<CatSegment>();

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

    public Level3Page()
    {
        InitializeComponent();
        InitGame();
    }

    private void InitGame()
    {
        _lives = 3;
        _isAnimating = false;
        if (GameOverFrame != null) GameOverFrame.IsVisible = false;
        _allCats.Clear();
        _gridMap = new int[Rows, Cols];

        UpdateStarsUI();
        _catsRemaining = TotalCatsToClear;
        if (RemainingLabel != null) RemainingLabel.Text = _catsRemaining.ToString();

        GameGrid.ColumnDefinitions.Clear();
        GameGrid.RowDefinitions.Clear();
        for (int i = 0; i < Cols; i++) GameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (int i = 0; i < Rows; i++) GameGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });


        // 1-4. External Group 1 
        CreateCat("Yellow", (1, 1), (2, 1), (3, 1)); 
        CreateCat("Lime", (10, 10), (9, 10), (8, 10)); 
        CreateCat("Cyan", (1, 10), (1, 9), (1, 8)); 
        CreateCat("Gold", (10, 1), (10, 2), (10, 3));

        // 5-8. Periphery Long-haired Cats
        CreateCat("Pink", (3, 8), (3, 7), (3, 6), (3, 5), (3, 4)); 
        CreateCat("Gray", (8, 3), (8, 4), (8, 5), (8, 6), (8, 7)); 
        CreateCat("Blue", (4, 3), (5, 3), (6, 3), (7, 3)); 
        CreateCat("Orange", (7, 8), (6, 8), (5, 8), (4, 8));

        // 9-12. Core Cross Lock
        CreateCat("Red", (5, 5), (6, 5)); 
        CreateCat("Green", (6, 6), (5, 6)); 
        CreateCat("Purple", (5, 6), (5, 5)); 
        CreateCat("Teal", (6, 5), (6, 6)); 

        RenderGrid();
    }
    // Create the body of the cat
    private void CreateCat(string colorKey, params (int r, int c)[] body)
    {
        for (int i = 0; i < body.Length; i++)
        {
            var seg = new CatSegment { Row = body[i].r, Col = body[i].c, IsHead = (i == 0), BodyIndex = i, ColorKey = colorKey };
            _allCats.Add(seg);
            _gridMap[seg.Row, seg.Col] = 1;
        }
    }

    private void RenderGrid()// Rendering of the grid
    {
        GameGrid.Children.Clear();
        foreach (var seg in _allCats.OrderBy(s => s.IsHead))
        {
            var container = new AbsoluteLayout();
            var btn = new Button
            {
                Text = seg.IsHead ? GetCatIcon(seg) : "●",
                BackgroundColor = GetColor(seg.ColorKey),
                TextColor = Colors.White,
                Padding = 0,
                FontSize = 14,
                CornerRadius = 8,
                // Determine which of the two controls should be displayed on top when they overlap on the screen
                ZIndex = seg.IsHead ? 10 : 0
            };
            // If it is a cat head, bind the click event to the button.
            if (seg.IsHead) btn.Clicked += OnCatHeadClicked;
            else btn.IsEnabled = false;

            container.Children.Add(btn);
            // Place the button body into the grid
            GameGrid.Add(container, seg.Col, seg.Row);
            seg.ButtonRef = btn;// Set the "btn" attribute for each "seg" in the following sequence
            seg.ContainerRef = container;
        }
    }


    // Direction Detection
    private string GetCatIcon(CatSegment head)// Assign value to the avatar image
    {
        // Search for the segment (neck) with BodyIndex 1 that belongs to this cat
        var neck = _allCats.FirstOrDefault(s => s.ColorKey == head.ColorKey && s.BodyIndex == 1);
        if (neck == null) return "😺";

        int dr = head.Row - neck.Row; 
        int dc = head.Col - neck.Col;
        // If the head minus the neck is greater than 0, it means flying upwards.
        // If the head is smaller than the neck, it means on the left.
        if (dr < 0) return "😺⬆️";
        if (dr > 0) return "😺⬇️";
        if (dc < 0) return "⬅️😺";
        return "😺➡️";
    }

    private Color GetColor(string name) => name switch// Set color
    {
        "Teal" => Color.FromUint(0xFF009688),
        "Pink" => Color.FromUint(0xFFE91E63),
        "Blue" => Color.FromUint(0xFF2196F3),
        "Orange" => Color.FromUint(0xFFFF9800),
        "Red" => Color.FromUint(0xFFF44336),
        "Green" => Color.FromUint(0xFF4CAF50),
        "Purple" => Color.FromUint(0xFF673AB7),
        "Gray" => Color.FromUint(0xFF9E9E9E),
        "Yellow" => Color.FromUint(0xFFFFEB3B),
        "Lime" => Color.FromUint(0xFFCDDC39),
        "Cyan" => Color.FromUint(0xFF00BCD4),
        "Gold" => Color.FromUint(0xFFFFC107),
        _ => Colors.Gray
    };

    private async void OnCatHeadClicked(object sender, EventArgs e)// The actions performed after clicking on the cat's head
    {
        if (_isAnimating || _lives <= 0) return;// If it is in animation or there is no heart, then stop.
        var head = _allCats.FirstOrDefault(s => s.ButtonRef == (Button)sender);// Find the first cat that clicks the button. If no cat is found, default to assigning the button to the cat.
        var body = _allCats.Where(s => s.ColorKey == head.ColorKey).OrderBy(s => s.BodyIndex).ToList();//Find bodies of the same color in the cat's head and arrange them in a row.

        // Calculate the flight direction
        int dr = Math.Sign(head.Row - body[1].Row);//Keep only the positive and negative values.
        int dc = Math.Sign(head.Col - body[1].Col);

        if (!IsPathClear(head, dr, dc, body))//If it doesn't work, it will explode.
        {
            await HandleCrash(head);
            return;
        }
        await FlyAway(body, dr, dc);
    }

    private bool IsPathClear(CatSegment head, int dr, int dc, List<CatSegment> body)//Check if the path is valid.
    {
        int r = head.Row + dr;//Start the detection from the last grid on the opposite side of the cat's face.
        int c = head.Col + dc;
        while (r >= 0 && r < Rows && c >= 0 && c < Cols)// When it does not exceed the boundaries of the game grid
        {
            if (!body.Any(b => b.Row == r && b.Col == c) && _gridMap[r, c] == 1) return false;//If the body grid of the cat does not have a value of 1 that is occupied,
                                                                                              //an error will be reported here.
            r += dr; c += dc;//Continue the inspection in the direction of the cat's head.
        }
        return true;
    }

      private async Task FlyAway(List<CatSegment> body, int dr, int dc)
  {
      _isAnimating = true;// Prevent animation from stopping upon click
        var tasks = body.Select(s => Task.WhenAll(
          s.ContainerRef.TranslateTo(dc * 1000, dr * 1000, 400, Easing.CubicIn),//Move 1000 pixels off the screen within 0.4 seconds, from slow to fast.
          s.ContainerRef.FadeTo(0, 400)//Become transparent
      ));
      await Task.WhenAll(tasks);//Wait until completion and then proceed.

        foreach (var s in body)// Remove object
        {
          _gridMap[s.Row, s.Col] = 0;
          _allCats.Remove(s);
          GameGrid.Children.Remove(s.ContainerRef);
      }

      _catsRemaining--;
      RemainingLabel.Text = _catsRemaining.ToString();
      if (_catsRemaining <= 0) ShowGameOver(true);// If all the cats have cleared, display the end screen.
        _isAnimating = false;
  }


    private async Task HandleCrash(CatSegment head)// Collision Effect
    {
        _isAnimating = true;
        head.ButtonRef.Text = "💥";// Change the cat's head into an explosion
        for (int i = 0; i < 2; i++) { await head.ButtonRef.TranslateTo(8, 0, 50); await head.ButtonRef.TranslateTo(-8, 0, 50); }//Move left and right twice to simulate the vibration.
        await head.ButtonRef.TranslateTo(0, 0, 50);// Return to original position
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
    "🏆 MISSION COMPLETE" : "😿 GAME OVER"
    ;
        GameOverSubText.Text = win ?
    "You win the game!" : "Try a different order next time!"
    ;

        // Notification
        if
     (win)
        {
            SendDelayedNotification();
        }
    }

    private void SendDelayedNotification()
    {
        var request = new
     NotificationRequest
        {
            NotificationId =
    3000, // Level 3 is distinguished by 3000.
            Title =
    "Fly A Cat - The end"
    ,
            Description =
    "Great! You have saved all the cats and have become a true hero！"
    ,
            Schedule =
    new
     NotificationRequestSchedule
    {
        NotifyTime = DateTime.Now.AddSeconds(
    10
    )
    }
        };

        LocalNotificationCenter.Current.Show(request);
    }

    private void OnRestartClicked(object sender, EventArgs e) => InitGame();
    private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}