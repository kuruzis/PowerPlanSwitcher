namespace PowerPlanSwitcher.RuleControl;

using RuleManagement.Dto;

public class GameRuleControl : UserControl
{
    private readonly List<string> games = [];
    private readonly TableLayoutPanel layout;
    private readonly CheckBox ChbAnyGame;
    private readonly Label LblGame;
    private readonly ComboBox CmbGame;
    private readonly Button BtnToggle;
    private readonly PictureBox PibGameInfo;
    private readonly ToolTip TipHints;

    private GameRuleDto dto = new();

    public GameRuleDto Dto
    {
        get
        {
            dto.Games = ChbAnyGame.Checked ? [] : [.. games];
            return dto;
        }

        set
        {
            dto = value;

            games.Clear();
            games.AddRange(dto.GetGameNames());

            RefreshGameItems();
            ChbAnyGame.Checked = games.Count == 0;
            CmbGame.Text = games.FirstOrDefault() ?? string.Empty;
            UpdateInputState();
        }
    }

    public GameRuleControl()
    {
        AutoSize = false;
        Margin = Padding.Empty;
        MinimumSize = new Size(0, 64);
        Size = new Size(645, 64);

        ChbAnyGame = new CheckBox
        {
            AutoSize = true,
            Margin = new Padding(3, 3, 3, 7),
            Text = "Any running game",
        };
        LblGame = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 6, 6, 3),
            Text = "Game:",
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
            ItemHeight = 24,
            Margin = new Padding(0, 3, 6, 3),
            MaxDropDownItems = 8,
        };
        BtnToggle = new Button
        {
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 3, 6, 3),
            Size = new Size(30, 25),
            Text = "+",
            UseVisualStyleBackColor = true,
        };
        PibGameInfo = new PictureBox
        {
            Anchor = AnchorStyles.Left,
            Image = Properties.Resources.info_rhombus,
            Margin = new Padding(0, 3, 3, 3),
            Size = new Size(24, 24),
            SizeMode = PictureBoxSizeMode.Zoom,
        };
        TipHints = new ToolTip
        {
            ShowAlways = true,
            UseAnimation = false,
            UseFading = false,
        };

        layout = new TableLayoutPanel
        {
            ColumnCount = 4,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RowCount = 2,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(ChbAnyGame, 0, 0);
        layout.SetColumnSpan(ChbAnyGame, 4);
        layout.Controls.Add(LblGame, 0, 1);
        layout.Controls.Add(CmbGame, 1, 1);
        layout.Controls.Add(BtnToggle, 2, 1);
        layout.Controls.Add(PibGameInfo, 3, 1);

        Controls.Add(layout);

        ChbAnyGame.CheckedChanged += ChbAnyGame_CheckedChanged;
        CmbGame.TextChanged += CmbGame_TextChanged;
        CmbGame.SelectedIndexChanged += CmbGame_SelectedIndexChanged;
        CmbGame.KeyDown += CmbGame_KeyDown;
        CmbGame.DrawItem += CmbGame_DrawItem;
        BtnToggle.Click += BtnToggle_Click;
        PibGameInfo.Click += PibGameInfo_Click;

        ApplyLocalization();
        ChbAnyGame.Checked = true;
        UpdateInputState();
    }

    private void ApplyLocalization()
    {
        ChbAnyGame.Text = Strings.GameRuleControl_ChbAnyGame;
        LblGame.Text = Strings.GameRuleControl_LblGame;

        TipHints.SetToolTip(
            ChbAnyGame,
            Strings.GameRuleControl_TipAny);
        TipHints.SetToolTip(
            CmbGame,
            Strings.GameRuleControl_TipGame);
        TipHints.SetToolTip(
            PibGameInfo,
            Strings.GameRuleControl_TipGame);
    }

    private void ChbAnyGame_CheckedChanged(object? sender, EventArgs e) =>
        UpdateInputState();

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
        var gameName = CmbGame.Text.Trim();
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
        var anyGame = ChbAnyGame.Checked;
        var gameName = CmbGame.Text.Trim();
        var isRegistered = games.Any(
            game => string.Equals(
                game,
                gameName,
                StringComparison.OrdinalIgnoreCase));

        LblGame.Enabled = !anyGame;
        CmbGame.Enabled = !anyGame;
        BtnToggle.Enabled = !anyGame && gameName.Length > 0;
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

    private void PibGameInfo_Click(object? sender, EventArgs e) =>
        TipHints.Show(
            TipHints.GetToolTip(PibGameInfo),
            PibGameInfo,
            0,
            PibGameInfo.Height,
            5000);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TipHints.Dispose();
        }

        base.Dispose(disposing);
    }
}
