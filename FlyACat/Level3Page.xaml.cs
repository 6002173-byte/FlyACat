using Microsoft.Maui.Controls;
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

        // ================= LEVEL 3: 螺旋死锁布局 (修复坐标) =================

        // 1-4. 角落清道夫 (头部, 身体1, 身体2...)
        CreateCat("Yellow", (1, 1), (2, 1), (3, 1)); // 向上飞 ⬆️
        CreateCat("Lime", (10, 10), (9, 10), (8, 10)); // 向下飞 ⬇️
        CreateCat("Cyan", (1, 10), (1, 9), (1, 8)); // 向右飞 ➡️
        CreateCat("Gold", (10, 1), (10, 2), (10, 3)); // 向左飞 ⬅️

        // 5-8. 外围长条猫 (风车)
        CreateCat("Pink", (3, 8), (3, 7), (3, 6), (3, 5), (3, 4)); // ➡️
        CreateCat("Gray", (8, 3), (8, 4), (8, 5), (8, 6), (8, 7)); // ⬅️
        CreateCat("Blue", (4, 3), (5, 3), (6, 3), (7, 3)); // ⬆️
        CreateCat("Orange", (7, 8), (6, 8), (5, 8), (4, 8)); // ⬇️

        // 9-12. 核心十字锁
        CreateCat("Red", (5, 5), (6, 5)); // ⬆️
        CreateCat("Green", (6, 6), (5, 6)); // ⬇️
        CreateCat("Purple", (5, 6), (5, 5)); // ➡️
        CreateCat("Teal", (6, 5), (6, 6)); // ⬅️

        RenderGrid();
    }

    private void CreateCat(string colorKey, params (int r, int c)[] body)
    {
        for (int i = 0; i < body.Length; i++)
        {
            var seg = new CatSegment { Row = body[i].r, Col = body[i].c, IsHead = (i == 0), BodyIndex = i, ColorKey = colorKey };
            _allCats.Add(seg);
            _gridMap[seg.Row, seg.Col] = 1;
        }
    }

    private void RenderGrid()
    {
        GameGrid.Children.Clear();
        var bodies = _allCats.Where(s => !s.IsHead).OrderBy(s => s.BodyIndex).ToList();
        var heads = _allCats.Where(s => s.IsHead).ToList();
        foreach (var seg in bodies) RenderSegment(seg, false);
        foreach (var seg in heads) RenderSegment(seg, true);
    }

    private void RenderSegment(CatSegment seg, bool isHead)
    {
        var container = new AbsoluteLayout();
        // 修正点：即使找不到第二节身体，也给一个基础猫头
        string displayText = isHead ? GetDirectionalCatIcon(seg) : "●";

        Button btn = new Button
        {
            CornerRadius = 8,
            FontSize = isHead ? 14 : 16, // 头部的字体稍微小点以容纳文字
            BackgroundColor = GetColorFromString(seg.ColorKey),
            Text = displayText,
            TextColor = Colors.White,
            HeightRequest = 36,
            WidthRequest = 36,
            Padding = 0,
            ZIndex = isHead ? 10 : 0
        };

        if (isHead) btn.Clicked += OnCatHeadClicked;
        else btn.IsEnabled = false;

        container.Children.Add(btn);
        GameGrid.Add(container, seg.Col, seg.Row);
        seg.ButtonRef = btn;
        seg.ContainerRef = container;
    }

    // 核心修复：更健壮的方向检测逻辑
    private string GetDirectionalCatIcon(CatSegment head)
    {
        var nextSeg = _allCats.FirstOrDefault(s => s.ColorKey == head.ColorKey && s.BodyIndex == 1);
        if (nextSeg == null) return "😺"; // 兜底显示

        int dr = head.Row - nextSeg.Row;
        int dc = head.Col - nextSeg.Col;

        // 根据头相对于第一节身体的位置判断起飞方向
        if (dr < 0 && dc == 0) return "😺⬆️";
        if (dr > 0 && dc == 0) return "😺⬇️";
        if (dr == 0 && dc < 0) return "⬅️😺";
        if (dr == 0 && dc > 0) return "😺➡️";

        return "😺";
    }

    private Color GetColorFromString(string name) => name switch
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

    private async void OnCatHeadClicked(object sender, EventArgs e)
    {
        if (_isAnimating || _lives <= 0) return;
        var headSeg = _allCats.FirstOrDefault(s => s.ButtonRef == (Button)sender);
        if (headSeg == null) return;

        var body = _allCats.Where(s => s.ColorKey == headSeg.ColorKey).OrderBy(s => s.BodyIndex).ToList();
        if (body.Count < 2) return;

        // 重新计算飞行方向
        int dr = Math.Sign(headSeg.Row - body[1].Row);
        int dc = Math.Sign(headSeg.Col - body[1].Col);

        if (!IsPathClear(headSeg, (dr, dc), body)) { await CrashAndPenalize(headSeg); return; }
        await FlyAndRemove(body, (dr, dc));
    }

    private bool IsPathClear(CatSegment head, (int dr, int dc) dir, List<CatSegment> myBody)
    {
        int r = head.Row + dir.dr, c = head.Col + dir.dc;
        while (r >= 0 && r < Rows && c >= 0 && c < Cols)
        {
            if (myBody.Any(s => s.Row == r && s.Col == c)) { r += dir.dr; c += dir.dc; continue; }
            if (_gridMap[r, c] == 1) return false;
            r += dir.dr; c += dir.dc;
        }
        return true;
    }

    private async Task FlyAndRemove(List<CatSegment> body, (int dr, int dc) dir)
    {
        _isAnimating = true;
        var tasks = body.Select(s => Task.WhenAll(
            s.ContainerRef.TranslateTo(dir.dc * 1200, dir.dr * 1200, 500, Easing.CubicIn),
            s.ContainerRef.FadeTo(0, 400)));

        await Task.WhenAll(tasks);

        foreach (var s in body)
        {
            _gridMap[s.Row, s.Col] = 0;
            _allCats.Remove(s);
            if (s.ContainerRef != null) GameGrid.Children.Remove(s.ContainerRef);
        }

        _catsRemaining--;
        RemainingLabel.Text = _catsRemaining.ToString();

        if (_catsRemaining <= 0) ShowGameOver(true);
        _isAnimating = false;
    }

    private async Task CrashAndPenalize(CatSegment head)
    {
        _isAnimating = true;
        var btn = head.ButtonRef;
        string oldText = btn.Text;
        btn.Text = "❌";

        for (int i = 0; i < 2; i++) { await btn.TranslateTo(10, 0, 50); await btn.TranslateTo(-10, 0, 50); }
        await btn.TranslateTo(0, 0, 50);

        btn.Text = oldText;
        _lives--; UpdateStarsUI();
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
        GameOverText.Text = win ? "🏆 MISSION COMPLETE" : "😿 GAME OVER";
        GameOverSubText.Text = win ? "You are the puzzle master!" : "Try a different order next time!";
    }

    private void OnRestartClicked(object sender, EventArgs e) => InitGame();
    private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}