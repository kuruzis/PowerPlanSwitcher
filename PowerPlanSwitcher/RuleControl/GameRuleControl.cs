namespace PowerPlanSwitcher.RuleControl;

using GameManagement;
using RuleManagement.Dto;

public class GameRuleControl : UserControl
{
    private readonly IGameMonitor? gameMonitor;
    private readonly List<string> detectedGames = [];

    private readonly TableLayoutPanel layout;
    private readonly TableLayoutPanel filterLayout;
    private readonly TableLayoutPanel detectedLayout;
    private readonly CheckBox ChbAnyGame;
    private readonly GameFilterEditor WhitelistEditor;
    private readonly GameFilterEditor BlacklistEditor;
    private readonly Label LblDetectedGames;
    private readonly ListBox LstDetectedGames;
    private readonly PictureBox PibGameInfo;
    private readonly ContextMenuStrip DetectedGamesMenu;
    private readonly ToolStripMenuItem MiAddToWhitelist;
    private readonly ToolStripMenuItem MiAddToBlacklist;
    private readonly ToolTip TipHints;
    private readonly System.Windows.Forms.Timer DetectedGamesTimer;

    private GameRuleDto dto = new();

    public GameRuleDto Dto
    {
        get
        {
            dto.Games = ChbAnyGame.Checked
                ? []
                : [.. WhitelistEditor.Games];
            dto.Blacklist = [.. BlacklistEditor.Games];
            return dto;
        }

        set
        {
            dto = value;

            var whitelist = dto.GetGameNames();
            WhitelistEditor.SetGames(whitelist);
            BlacklistEditor.SetGames(dto.GetBlacklistNames());
            ChbAnyGame.Checked = whitelist.Count == 0;
            UpdateInputState();
        }
    }

    public GameRuleControl(IGameMonitor? gameMonitor = null)
    {
        this.gameMonitor = gameMonitor;

        AutoSize = false;
        Margin = Padding.Empty;
        MinimumSize = new Size(0, 100);
        Size = new Size(645, 100);

        ChbAnyGame = new CheckBox
        {
            AutoSize = true,
            Margin = new Padding(3, 2, 3, 4),
            Text = "Any running game",
        };
        WhitelistEditor = new GameFilterEditor
        {
            Dock = DockStyle.Fill,
        };
        BlacklistEditor = new GameFilterEditor
        {
            Dock = DockStyle.Fill,
        };
        LblDetectedGames = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 2, 4, 2),
            Text = "Detected games:",
        };
        LstDetectedGames = new ListBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom
                | AnchorStyles.Left | AnchorStyles.Right,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
            Margin = new Padding(3, 0, 3, 2),
        };
        PibGameInfo = new PictureBox
        {
            Anchor = AnchorStyles.Right,
            Image = Properties.Resources.info_rhombus,
            Margin = new Padding(0, 0, 2, 0),
            Size = new Size(20, 20),
            SizeMode = PictureBoxSizeMode.Zoom,
        };
        TipHints = new ToolTip
        {
            ShowAlways = true,
            UseAnimation = false,
            UseFading = false,
        };
        MiAddToWhitelist = new ToolStripMenuItem("Add to whitelist");
        MiAddToBlacklist = new ToolStripMenuItem("Add to blacklist");
        DetectedGamesMenu = new ContextMenuStrip();
        DetectedGamesMenu.Items.AddRange(
            [MiAddToWhitelist, MiAddToBlacklist]);

        filterLayout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0),
            Padding = Padding.Empty,
            RowCount = 3,
        };
        filterLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        filterLayout.Controls.Add(ChbAnyGame, 0, 0);
        filterLayout.Controls.Add(WhitelistEditor, 0, 1);
        filterLayout.Controls.Add(BlacklistEditor, 0, 2);

        detectedLayout = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RowCount = 2,
        };
        detectedLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));
        detectedLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.AutoSize));
        detectedLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detectedLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));
        detectedLayout.Controls.Add(LblDetectedGames, 0, 0);
        detectedLayout.Controls.Add(PibGameInfo, 1, 0);
        detectedLayout.Controls.Add(LstDetectedGames, 0, 1);
        detectedLayout.SetColumnSpan(LstDetectedGames, 2);

        layout = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RowCount = 1,
        };
        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 58F));
        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 42F));
        layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(filterLayout, 0, 0);
        layout.Controls.Add(detectedLayout, 1, 0);

        Controls.Add(layout);

        ChbAnyGame.CheckedChanged += ChbAnyGame_CheckedChanged;
        PibGameInfo.Click += PibGameInfo_Click;
        LstDetectedGames.ContextMenuStrip = DetectedGamesMenu;
        LstDetectedGames.DoubleClick += LstDetectedGames_DoubleClick;
        DetectedGamesMenu.Opening += DetectedGamesMenu_Opening;
        MiAddToWhitelist.Click += MiAddToWhitelist_Click;
        MiAddToBlacklist.Click += MiAddToBlacklist_Click;
        VisibleChanged += GameRuleControl_VisibleChanged;

        DetectedGamesTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000,
        };
        DetectedGamesTimer.Tick += DetectedGamesTimer_Tick;

        ApplyLocalization();
        ChbAnyGame.Checked = true;
        UpdateInputState();
    }

    private void ApplyLocalization()
    {
        ChbAnyGame.Text = Strings.GameRuleControl_ChbAnyGame;
        WhitelistEditor.Title = Strings.GameRuleControl_LblWhitelist;
        BlacklistEditor.Title = Strings.GameRuleControl_LblBlacklist;
        LblDetectedGames.Text =
            Strings.GameRuleControl_LblDetectedGames;
        MiAddToWhitelist.Text =
            Strings.GameRuleControl_AddToWhitelist;
        MiAddToBlacklist.Text =
            Strings.GameRuleControl_AddToBlacklist;

        TipHints.SetToolTip(
            ChbAnyGame,
            Strings.GameRuleControl_TipAny);
        TipHints.SetToolTip(
            LblDetectedGames,
            Strings.GameRuleControl_TipDetected);
        TipHints.SetToolTip(
            LstDetectedGames,
            Strings.GameRuleControl_TipDetected);
        TipHints.SetToolTip(
            PibGameInfo,
            Strings.GameRuleControl_TipDetected);

        WhitelistEditor.SetHint(
            Strings.GameRuleControl_TipWhitelist);
        BlacklistEditor.SetHint(
            Strings.GameRuleControl_TipBlacklist);
    }

    private void ChbAnyGame_CheckedChanged(
        object? sender,
        EventArgs e) =>
        UpdateInputState();

    private void UpdateInputState() =>
        WhitelistEditor.SetInputEnabled(!ChbAnyGame.Checked);

    private void GameRuleControl_VisibleChanged(
        object? sender,
        EventArgs e) =>
        UpdateDetectedGamesTimer();

    private void UpdateDetectedGamesTimer()
    {
        if (Visible && gameMonitor is not null)
        {
            RefreshDetectedGames();

            if (!DetectedGamesTimer.Enabled)
            {
                DetectedGamesTimer.Start();
            }

            return;
        }

        DetectedGamesTimer.Stop();
    }

    private void DetectedGamesTimer_Tick(
        object? sender,
        EventArgs e) =>
        RefreshDetectedGames();

    private void RefreshDetectedGames()
    {
        if (gameMonitor is null)
        {
            return;
        }

        List<string> games;

        try
        {
            games =
            [
                .. gameMonitor.GetRunningGames()
                    .Select(game => GameExecutableName.Normalize(
                        game.ExecutableName))
                    .Where(name => name.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(
                        name => name,
                        StringComparer.OrdinalIgnoreCase)
            ];
        }
        catch
        {
            return;
        }

        if (games.SequenceEqual(
            detectedGames,
            StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var selectedGame = LstDetectedGames.SelectedItem as string;

        detectedGames.Clear();
        detectedGames.AddRange(games);

        LstDetectedGames.BeginUpdate();
        LstDetectedGames.Items.Clear();
        LstDetectedGames.Items.AddRange(
            [.. detectedGames.Cast<object>()]);
        LstDetectedGames.EndUpdate();

        if (selectedGame is not null)
        {
            var selectedIndex = detectedGames.FindIndex(
                game => string.Equals(
                    game,
                    selectedGame,
                    StringComparison.OrdinalIgnoreCase));

            if (selectedIndex >= 0)
            {
                LstDetectedGames.SelectedIndex = selectedIndex;
            }
        }
    }

    private void DetectedGamesMenu_Opening(
        object? sender,
        System.ComponentModel.CancelEventArgs e)
    {
        var hasSelection = LstDetectedGames.SelectedItem is string;
        MiAddToWhitelist.Enabled = hasSelection;
        MiAddToBlacklist.Enabled = hasSelection;
    }

    private void LstDetectedGames_DoubleClick(
        object? sender,
        EventArgs e) =>
        AddSelectedGameToWhitelist();

    private void MiAddToWhitelist_Click(
        object? sender,
        EventArgs e) =>
        AddSelectedGameToWhitelist();

    private void MiAddToBlacklist_Click(
        object? sender,
        EventArgs e) =>
        AddSelectedGameToBlacklist();

    private void AddSelectedGameToWhitelist()
    {
        if (LstDetectedGames.SelectedItem is not string game)
        {
            return;
        }

        if (ChbAnyGame.Checked)
        {
            ChbAnyGame.Checked = false;
        }

        WhitelistEditor.AddGame(game);
    }

    private void AddSelectedGameToBlacklist()
    {
        if (LstDetectedGames.SelectedItem is not string game)
        {
            return;
        }

        BlacklistEditor.AddGame(game);
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
            DetectedGamesTimer.Dispose();
            DetectedGamesMenu.Dispose();
            TipHints.Dispose();
        }

        base.Dispose(disposing);
    }
}
