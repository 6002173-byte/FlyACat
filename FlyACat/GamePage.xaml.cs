using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlyACat;

public partial class GamePage : ContentPage
{
    // 戏配置 
    private const int Rows = 12;
    private const int Cols = 12;
    private const int TotalCatsToClear = 5;

    // 状态 
    private int _lives = 3;
    private int _catsRemaining = 0;
    private bool _isAnimating = false;

    // 数据结构 
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

    private List<CatSegment> _allCats = new List<CatSegment>();
    private int[,] _gridMap;

    public GamePage()
    {
        InitializeComponent();
        InitGame();
    }

    private void InitGame()
    {
        _lives = 3;
        _isAnimating = false;
        GameOverFrame.IsVisible = false;
        _allCats.Clear();

        UpdateStarsUI();
        _catsRemaining = TotalCatsToClear;
        RemainingLabel.Text = _catsRemaining.ToString();

        // 初始化网格定义
        GameGrid.ColumnDefinitions.Clear();
        GameGrid.RowDefinitions.Clear();
        for (int i = 0; i < Cols; i++)
            GameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (int i = 0; i < Rows; i++)
            GameGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

        _gridMap = new int[Rows, Cols];

        // 关卡数据...
        // 箭头方向自动计算为从头指向远离身体的方向

        // 1. 青色猫 (身体在下，所以向上飞)
        // 头(1,1), 身(2,1) -> 身体在下方 -> 箭头向上 ⬆️
        CreateCat("Teal", (1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (6, 1), (7, 1), (8, 1), (9, 1));

        // 2. 粉色猫 
        // 头(9,10), 身(8,10) -> 身体在上方 -> 箭头向下 
        CreateCat("Pink", (9, 10), (8, 10), (7, 10), (6, 10), (5, 10), (4, 10), (3, 10), (2, 10));

        // 3. 蓝色猫 (顶部横向，头在左，身体在右 -> 箭头向左 ⬅️)
        CreateCat("Blue", (1, 3), (1, 4), (1, 5), (1, 6), (1, 7), (2, 7), (3, 7), (4, 7), (5, 7), (6, 7), (7, 7), (8, 7));

        // 4. 黄色猫 (底部横向长条，头在右，身体在左 -> 箭头向右 ➡️)
        CreateCat("Yellow", (9, 9), (9, 8), (9, 7), (9, 6), (9, 5), (9, 4), (9, 3), (9, 2), (8, 2), (7, 2), (6, 2), (5, 2), (4, 2), (3, 2), (2, 2), (1, 2));

        // 5. 紫色猫 (中间短竖条，头在上，身体在下 -> 箭头向上 ⬆️)
        CreateCat("Magenta", (3, 4), (4, 4), (5, 4), (6, 4), (7, 4), (7, 5), (7, 6));

        RenderGrid();
    }

    // 创建猫，并按顺序赋予 BodyIndex

    private void CreateCat(string colorKey, params (int r, int c)[] body)
    {
        for (int i = 0; i < body.Length; i++)
        {
            var seg = new CatSegment
            {
                Row = body[i].r,
                Col = body[i].c,
                IsHead = (i == 0),
                BodyIndex = i, 
                ColorKey = colorKey
            };
            _allCats.Add(seg);
            _gridMap[seg.Row, seg.Col] = 1; // 标记网格占用
        }
    }

    private void RenderGrid()
    {
        GameGrid.Children.Clear();

        // 先画所有身体，最后画所有头 -> 保证头在最上层 
        var bodies = _allCats.Where(s => !s.IsHead).OrderBy(s => s.BodyIndex).ToList();
        var heads = _allCats.Where(s => s.IsHead).ToList();

        foreach (var seg in bodies)
        {
            RenderSegment(seg, false);
        }

        foreach (var seg in heads)
        {
            RenderSegment(seg, true);
        }
    }

    private void RenderSegment(CatSegment seg, bool isHead)
    {
        var container = new AbsoluteLayout();

        // 头部显示带箭头的图标，身体显示圆点
        string displayText = isHead ? GetDirectionalCatIcon(seg) : "🟢";

        Button btn = new Button
        {
            CornerRadius = 10,
            FontSize = 18,
            AnchorX = 0.5,
            AnchorY = 0.5,
            Padding = 0,
            Margin = 0,
            BackgroundColor = GetColorFromString(seg.ColorKey),
            Text = displayText,
            TextColor = Colors.White,
            HeightRequest = 40,
            WidthRequest = 40,
            BorderWidth = 0,
            ZIndex = isHead ? 10 : 0 // 头部层级最高
        };

        if (isHead)
        {
            btn.Clicked += OnCatHeadClicked;
        }
        else
        {
            btn.IsEnabled = false; // 身体不可点击
        }

        container.Children.Add(btn);
        GameGrid.Add(container, seg.Col, seg.Row);

        seg.ButtonRef = btn;
        seg.ContainerRef = container;
    }

    
    // 根据 BodyIndex=1 的身体段计算箭头方向
    
    private string GetDirectionalCatIcon(CatSegment head)
    {
        // 直接找 BodyIndex=1 的段
        var secondSeg = _allCats.FirstOrDefault(s => s.ColorKey == head.ColorKey && s.BodyIndex == 1);
        if (secondSeg == null) return "😺";

        // 计算向量：头 - 第二节身体 = 飞行方向
        int dr = head.Row - secondSeg.Row;
        int dc = head.Col - secondSeg.Col;

        //(-1, 0, 1)
        if (dr != 0) dr = dr / Math.Abs(dr);
        if (dc != 0) dc = dc / Math.Abs(dc);

        return (dr, dc) switch
        {
            (0, 1) => "😺➡️",   // 向右飞
            (0, -1) => "⬅️😺",  // 向左飞
            (1, 0) => "😺⬇️",   // 向下飞
            (-1, 0) => "⬆️",  // 向上飞
            _ => "😺"           // 默认
        };
    }

    private Color GetColorFromString(string colorName)
    {
        return colorName switch
        {
            "Teal" => Color.FromHex("#009688"),
            "Pink" => Color.FromHex("#E91E63"), 
            "Blue" => Color.FromHex("#2196F3"),
            "Yellow" => Color.FromHex("#FFEB3B"),
            "Magenta" => Color.FromHex("#9C27B0"),
            _ => Colors.Gray
        };
    }

    private async void OnCatHeadClicked(object sender, EventArgs e)
    {
        if (_isAnimating || _lives <= 0) return;

        Button clickedBtn = sender as Button;
        if (clickedBtn == null) return;

        var headSeg = _allCats.FirstOrDefault(s => s.ButtonRef == clickedBtn);
        if (headSeg == null) return;

        // 按 BodyIndex 获取完整身体链，确保顺序正确
        var catBody = GetFullCatBodyByIndex(headSeg);
        if (catBody.Count < 2) return;

        // 获取飞行方向
        var secondSeg = catBody[1];
        int dr = headSeg.Row - secondSeg.Row;
        int dc = headSeg.Col - secondSeg.Col;

        if (dr != 0) dr = dr / Math.Abs(dr);
        if (dc != 0) dc = dc / Math.Abs(dc);

        var flightDir = (dr, dc);

        // 检查路径，不通则撞墙，禁止反向飞
        if (!IsPathClear(headSeg, flightDir, catBody))
        {
            await CrashAndPenalize(headSeg);
            return;
        }

        await FlyAndRemove(catBody, flightDir);
    }

    
    // 按 BodyIndex 顺序获取同色猫的所有身体段
    private List<CatSegment> GetFullCatBodyByIndex(CatSegment head)
    {
        return _allCats
            .Where(s => s.ColorKey == head.ColorKey)
            .OrderBy(s => s.BodyIndex)
            .ToList();
    }

    
    ///检查路径是否通畅
    private bool IsPathClear(CatSegment head, (int dr, int dc) dir, List<CatSegment> myBody)
    {
        int r = head.Row + dir.dr;
        int c = head.Col + dir.dc;

        while (r >= 0 && r < Rows && c >= 0 && c < Cols)
        {
            // 跳过自己的身体
            if (myBody.Any(s => s.Row == r && s.Col == c))
            {
                r += dir.dr;
                c += dir.dc;
                continue;
            }

            // 碰到其他障碍物
            if (_gridMap[r, c] == 1)
            {
                return false;
            }

            r += dir.dr;
            c += dir.dc;
        }

        return true; // 成功飞出边界
    }

    private async Task FlyAndRemove(List<CatSegment> catBody, (int dr, int dc) dir)
    {
        _isAnimating = true;

        // 计算飞行距离 
        double targetX = dir.dc * 20 * 40;
        double targetY = dir.dr * 20 * 40;

        var tasks = new List<Task>();

        foreach (var seg in catBody)
        {
            if (seg.ContainerRef != null)
            {
                tasks.Add(seg.ContainerRef.TranslateTo(targetX, targetY, 400, Easing.CubicIn));
                tasks.Add(seg.ContainerRef.FadeTo(0, 400));
            }
        }

        await Task.WhenAll(tasks);

        // 清理数据
        foreach (var seg in catBody)
        {
            _gridMap[seg.Row, seg.Col] = 0;
            _allCats.Remove(seg);
            if (seg.ContainerRef != null)
                GameGrid.Children.Remove(seg.ContainerRef);
        }

        _catsRemaining--;
        RemainingLabel.Text = _catsRemaining.ToString();

        if (_catsRemaining <= 0)
        {
            await Task.Delay(300);
            ShowGameOver(true);
        }

        _isAnimating = false;
    }

    private async Task CrashAndPenalize(CatSegment head)
    {
        _isAnimating = true;
        var btn = head.ButtonRef;
        var originalBg = btn.BackgroundColor;
        var originalText = btn.Text;

        // 撞击特效
        btn.BackgroundColor = Colors.Red;
        btn.Text = "💥";

        await btn.TranslateTo(5, 0, 50);
        await btn.TranslateTo(-5, 0, 50);
        await btn.TranslateTo(5, 0, 50);
        await btn.TranslateTo(0, 0, 50);

        await Task.Delay(300);

        // 恢复原状
        btn.BackgroundColor = originalBg;
        btn.Text = originalText;

        _lives--;
        UpdateStarsUI();

        if (_lives <= 0)
        {
            await Task.Delay(500);
            ShowGameOver(false);
        }

        _isAnimating = false;
    }

    private void UpdateStarsUI()
    {
        Star1.Text = _lives >= 1 ? "❤️" : "🖤";
        Star2.Text = _lives >= 2 ? "❤️" : "🖤";
        Star3.Text = _lives >= 3 ? "❤️" : "🖤";
    }

    private void ShowGameOver(bool isWin)
    {
        GameOverFrame.IsVisible = true;
        if (isWin)
        {
            GameOverText.Text = "🎉 You have successfully cleared the level.！";
            GameOverSubText.Text = "All the cats flew away safely.";
            RetryButton.BackgroundColor = Color.FromArgb("#4CAF50");
        }
        else
        {
            GameOverText.Text = "💥The game was a failure. ";
            GameOverSubText.Text = "The cat suffered too many impacts and its life ran out...";
            RetryButton.BackgroundColor = Color.FromArgb("#FF5722");
        }
    }

    private async void OnRestartClicked(object sender, EventArgs e) => InitGame();

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
