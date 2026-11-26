namespace PowerPlanSwitcher.RuleControl;

using GameManagement;

public class GameFilterEditor : UserControl
{
    private readonly List<string> games = [];
    private readonly TableLayoutPanel layout;
    private readonly Label LblTitle;
    private readonly ComboBox CmbGame;
    private readonly Button BtnToggle;
    private readonly ToolTip TipHints;

    private bool inputEnabled = true;

    public event EventHandler? GamesChanged;

    public string Title
    {
        get => LblTitle.Text;
        set => LblTitle.Text = value;
    }

    public IReadOnlyList<string> Games => games;

    public GameFilterEditor()
    {
        AutoSize = false;
        Margin = Padding.Empty;
        MinimumSize = new Size(0, 25);
        Size = new Size(380, 25);

        LblTitle = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 4, 4, 1),
            Text = "Filter:",
        };
        CmbGame = new ComboBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            DrawMode = DrawMode.OwnerDrawFixed,
            DropDownHeight = 180,
            DropDownStyle = ComboBoxStyle.DropDown,
            FormattingEnabled = true,
            ItemHeight = 22,
            Margin = new Padding(0, 1, 4, 1),
            MaxDropDownItems = 8,
        };
        BtnToggle = new Button
        {
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 1, 3, 1),
            Size = new Size(28, 23),
            Text = "+",
            UseVisualStyleBackColor = true,
        };
        TipHints = new ToolTip
        {
            ShowAlways = true,
            UseAnimation = false,
            UseFading = false,
        };

        layout = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RowCount = 1,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(LblTitle, 0, 0);
        layout.Controls.Add(CmbGame, 1, 0);
        layout.Controls.Add(BtnToggle, 2, 0);

        Controls.Add(layout);

        CmbGame.TextChanged += CmbGame_TextChanged;
        CmbGame.SelectedIndexChanged += CmbGame_SelectedIndexChanged;
        CmbGame.KeyDown += CmbGame_KeyDown;
        CmbGame.DrawItem += CmbGame_DrawItem;
        BtnToggle.Click += BtnToggle_Click;

        UpdateInputState();
    }

    public void SetGames(IEnumerable<string> values)
    {
        games.Clear();
        games.AddRange(
            values
                .Select(GameExecutableName.Normalize)
                .Where(name => name.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase));

        RefreshGameItems();
        CmbGame.Text = games.FirstOrDefault() ?? string.Empty;
        UpdateInputState();
    }

    public void AddGame(string gameName)
    {
        var normalizedName = GameExecutableName.Normalize(gameName);
        if (normalizedName.Length == 0)
        {
            return;
        }

        if (!games.Any(game => string.Equals(
            game,
            normalizedName,
            StringComparison.OrdinalIgnoreCase)))
        {
            games.Add(normalizedName);
        }

        CmbGame.Text = normalizedName;
        RefreshGameItems();
        UpdateInputState();
        GamesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        UpdateInputState();
    }

    public void SetHint(string hint)
    {
        TipHints.SetToolTip(LblTitle, hint);
        TipHints.SetToolTip(CmbGame, hint);
    }

    private void CmbGame_TextChanged(object? sender, EventArgs e) =>
        UpdateInputState();

    private void CmbGame_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (CmbGame.SelectedItem is string game)
        {
            CmbGame.Text = game;
        }

        UpdateInputState();
    }

    private void CmbGame_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        ToggleSelectedGame();
    }

    private void BtnToggle_Click(object? sender, EventArgs e) =>
        ToggleSelectedGame();

    private void ToggleSelectedGame()
    {
        if (!inputEnabled)
        {
            return;
        }

        var gameName = GameExecutableName.Normalize(CmbGame.Text);
        if (gameName.Length == 0)
        {
            return;
        }

        var existingIndex = games.FindIndex(
            game => string.Equals(
                game,
                gameName,
                StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            games.RemoveAt(existingIndex);
        }
        else
        {
            games.Add(gameName);
        }

        CmbGame.Text = gameName;
        RefreshGameItems();
        UpdateInputState();
        GamesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshGameItems()
    {
        CmbGame.BeginUpdate();
        CmbGame.Items.Clear();
        CmbGame.Items.AddRange([.. games.Cast<object>()]);
        CmbGame.EndUpdate();
    }

    private void UpdateInputState()
    {
        var gameName = CmbGame.Text.Trim();
        var isRegistered = games.Any(
            game => string.Equals(
                game,
                gameName,
                StringComparison.OrdinalIgnoreCase));

        LblTitle.Enabled = inputEnabled;
        CmbGame.Enabled = inputEnabled;
        BtnToggle.Enabled = inputEnabled && gameName.Length > 0;
        BtnToggle.Text = isRegistered ? "−" : "+";

        TipHints.SetToolTip(
            BtnToggle,
            isRegistered
                ? Strings.GameRuleControl_RemoveTip
                : Strings.GameRuleControl_AddTip);
    }

    private void CmbGame_DrawItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();

        if (e.Index < 0 || e.Index >= CmbGame.Items.Count)
        {
            return;
        }

        var text = CmbGame.Items[e.Index]?.ToString() ?? string.Empty;
        var bounds = new Rectangle(
            e.Bounds.X + 1,
            e.Bounds.Y + 1,
            e.Bounds.Width - 2,
            e.Bounds.Height - 2);

        using var borderPen = new Pen(SystemColors.ControlDark);
        e.Graphics.DrawRectangle(borderPen, bounds);

        var textBounds = Rectangle.Inflate(bounds, -4, 0);
        var textColor = (e.State & DrawItemState.Selected) != 0
            ? SystemColors.HighlightText
            : SystemColors.ControlText;

        TextRenderer.DrawText(
            e.Graphics,
            text,
            e.Font ?? CmbGame.Font,
            textBounds,
            textColor,
            TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TipHints.Dispose();
        }

        base.Dispose(disposing);
    }
}
