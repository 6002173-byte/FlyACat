using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlyACat;

public partial class Level2Page : ContentPage
{
    private const int Rows = 12;
    private const int Cols = 12;
    private int _lives = 3;
    private int _catsRemaining = 9; // 第二关有9只猫
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

    public Level2Page()
    {
        InitializeComponent();
        InitGame();
    }

    private void InitGame()
    {
        _lives = 3;
        _isAnimating = false;
        _catsRemaining = 9;
        _allCats.Clear();
        _gridMap = new int[Rows, Cols];

        GameOverFrame.IsVisible = false;
        NextLevelButton.IsVisible = false;
        RemainingLabel.Text = _catsRemaining.ToString();
        UpdateStarsUI();

        GameGrid.ColumnDefinitions.Clear();
        GameGrid.RowDefinitions.Clear();
        for (int i = 0; i < Cols; i++) GameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (int i = 0; i < Rows; i++) GameGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

        // --- 修正后的猫咪坐标（第一个坐标是头，第二个是脖子） ---
        // 确保第一个点和第二个点的距离只有 1 格
        CreateCat("Pink", (2, 9), (2, 8), (2, 7));    // 右 -> 左 (头在9，脖在8)
        CreateCat("Gray", (9, 1), (8, 1), (7, 1));    // 下 -> 上 (头在9，脖在8)
        CreateCat("Blue", (4, 3), (4, 4), (4, 5));    // 左 -> 右 (头在3，脖在4)
        CreateCat("Green", (5, 10), (6, 10), (7, 10)); // 上 -> 下
        CreateCat("Orange", (10, 8), (10, 7), (10, 6));
        CreateCat("Red", (3, 5), (4, 5), (5, 5));
        CreateCat("Purple", (5, 3), (5, 4), (5, 5));
        CreateCat("Teal", (7, 4), (6, 4), (5, 4));
        CreateCat("Brown", (6, 7), (6, 6), (6, 5));

        RenderGrid();
    }

    private void CreateCat(string color, params (int r, int c)[] body)
    {
        for (int i = 0; i < body.Length; i++)
        {
            var seg = new CatSegment { Row = body[i].r, Col = body[i].c, IsHead = (i == 0), BodyIndex = i, ColorKey = color };
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
                Text = seg.IsHead ? GetCatIcon(seg) : "●",
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
        // 查找属于这只猫的 BodyIndex 为 1 的段（脖子）
        var neck = _allCats.FirstOrDefault(s => s.ColorKey == head.ColorKey && s.BodyIndex == 1);
        if (neck == null) return "😺";

        int dr = head.Row - neck.Row; // 行差
        int dc = head.Col - neck.Col; // 列差

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

        // 计算飞行方向
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
            s.ContainerRef.TranslateTo(dc * 1000, dr * 1000, 400, Easing.CubicIn),
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

    private async Task HandleCrash(CatSegment head)
    {
        _isAnimating = true;
        head.ButtonRef.Text = "💥";
        for (int i = 0; i < 2; i++) { await head.ButtonRef.TranslateTo(8, 0, 50); await head.ButtonRef.TranslateTo(-8, 0, 50); }
        await head.ButtonRef.TranslateTo(0, 0, 50);
        head.ButtonRef.Text = GetCatIcon(head);
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
        GameOverFrame.IsVisible = true;
        GameOverText.Text = win ? "🌟 CLEAR!" : "💥 FAILED";
        NextLevelButton.IsVisible = win;
    }

    private Color GetColor(string key) => key switch
    {
        "Teal" => Colors.Teal,
        "Pink" => Colors.DeepPink,
        "Blue" => Colors.DodgerBlue,
        "Orange" => Colors.Orange,
        "Red" => Colors.Red,
        "Green" => Colors.Green,
        "Purple" => Colors.Purple,
        "Gray" => Colors.Gray,
        "Brown" => Colors.Brown,
        _ => Colors.Gray
    };

    private async void OnNextLevelClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Level3Page());
    private void OnRestartClicked(object sender, EventArgs e) => InitGame();
    private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}