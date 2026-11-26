namespace RuleManagementTest;

using System.Globalization;
using FakeItEasy;
using GameManagement;
using RuleManagement.Dto;
using RuleManagement.Rules;

[TestClass]
public sealed class GameRuleTest
{
    private IGameMonitor gameMonitor = null!;
    private CultureInfo? previousUiCulture;

    [TestInitialize]
    public void Setup()
    {
        gameMonitor = A.Fake<IGameMonitor>();
        previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (previousUiCulture is not null)
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private static IGame CreateGame(int processId, string executableName)
    {
        var game = A.Fake<IGame>();
        _ = A.CallTo(() => game.ProcessId).Returns(processId);
        _ = A.CallTo(() => game.ExecutableName).Returns(executableName);
        return game;
    }

    [TestMethod]
    public void StartRuling_AnyGame_MatchesAllRunningGames()
    {
        var firstGame = CreateGame(1, "Game 1.exe");
        var secondGame = CreateGame(2, "Game 2.exe");
        _ = A.CallTo(() => gameMonitor.GetRunningGames())
            .Returns([firstGame, secondGame]);

        var rule = new GameRule(
            gameMonitor,
            new GameRuleDto { SchemeGuid = Guid.NewGuid() });
        rule.StartRuling();

        Assert.AreEqual(
            2,
            rule.TriggerCount,
            "An empty filter should match every running game.");
    }

    [TestMethod]
    public void StartRuling_WithFilter_MatchesOnlyListedGames()
    {
        var matchingGame = CreateGame(1, "Game 1.exe");
        var otherGame = CreateGame(2, "Game 2.exe");
        _ = A.CallTo(() => gameMonitor.GetRunningGames())
            .Returns([matchingGame, otherGame]);

        var rule = new GameRule(
            gameMonitor,
            new GameRuleDto
            {
                Games = ["Game 1.exe"],
                SchemeGuid = Guid.NewGuid(),
            });
        rule.StartRuling();

        Assert.AreEqual(1, rule.TriggerCount);
    }

    [TestMethod]
    public void StartRuling_FilterIsCaseInsensitive()
    {
        var game = CreateGame(1, "Game 1.exe");
        _ = A.CallTo(() => gameMonitor.GetRunningGames())
            .Returns([game]);

        var rule = new GameRule(
            gameMonitor,
            new GameRuleDto
            {
                Games = ["game 1.EXE"],
                SchemeGuid = Guid.NewGuid(),
            });
        rule.StartRuling();

        Assert.AreEqual(1, rule.TriggerCount);
    }

    [TestMethod]
    public void StartRuling_FilterMatchesExecutableName_WhenMonitorReturnsFullPath()
    {
        var game = CreateGame(1, @"C:\Games\Game 1.exe");
        _ = A.CallTo(() => gameMonitor.GetRunningGames())
            .Returns([game]);

        var rule = new GameRule(
            gameMonitor,
            new GameRuleDto
            {
                Games = ["Game 1.exe"],
                SchemeGuid = Guid.NewGuid(),
            });
        rule.StartRuling();

        Assert.AreEqual(1, rule.TriggerCount);
    }

    [TestMethod]
    public void StartRuling_FilterWithFullPath_MatchesExecutableName()
    {
        var game = CreateGame(1, "Game 1.exe");
        _ = A.CallTo(() => gameMonitor.GetRunningGames())
            .Returns([game]);

        var rule = new GameRule(
            gameMonitor,
            new GameRuleDto
            {
                Games = [@"C:\Games\Game 1.exe"],
                SchemeGuid = Guid.NewGuid(),
            });
        rule.StartRuling();

        Assert.AreEqual(1, rule.TriggerCount);
    }

    [TestMethod]
    public void GameStarted_Matching_IncrementsTriggerCount()
    {
        var rule = CreateStartedRuleWithFilter("Game 1.exe");
        var game = CreateGame(1, "Game 1.exe");

        gameMonitor.GameStarted += Raise.With(new GameEventArgs(game));

        Assert.AreEqual(1, rule.TriggerCount);
    }

    [TestMethod]
    public void GameStarted_NotMatching_DoesNotIncrementTriggerCount()
    {
        var rule = CreateStartedRuleWithFilter("Game 1.exe");
        var game = CreateGame(1, "Game 2.exe");

        gameMonitor.GameStarted += Raise.With(new GameEventArgs(game));

        Assert.AreEqual(0, rule.TriggerCount);
    }

    [TestMethod]
    public void GameTerminated_Matching_DecrementsTriggerCount()
    {
        var game = CreateGame(1, "Game 1.exe");
        _ = A.CallTo(() => gameMonitor.GetRunningGames())
            .Returns([game]);

        var rule = new GameRule(
            gameMonitor,
            new GameRuleDto
            {
                Games = ["Game 1.exe"],
                SchemeGuid = Guid.NewGuid(),
            });
        rule.StartRuling();
        Assert.AreEqual(1, rule.TriggerCount);

        gameMonitor.GameTerminated += Raise.With(new GameEventArgs(game));

        Assert.AreEqual(0, rule.TriggerCount);
    }

    [TestMethod]
    public void GameTerminated_NotMatched_DoesNotDecrementTriggerCount()
    {
        var game = CreateGame(1, "Game 1.exe");
        var rule = new GameRule(
            gameMonitor,
            new GameRuleDto
            {
                Games = ["Game 2.exe"],
                SchemeGuid = Guid.NewGuid(),
            });
        rule.StartRuling();

        gameMonitor.GameTerminated += Raise.With(new GameEventArgs(game));

        Assert.AreEqual(0, rule.TriggerCount);
    }

    [TestMethod]
    public void MultipleInstances_CountEachProcess()
    {
        var firstInstance = CreateGame(1, "Game 1.exe");
        var secondInstance = CreateGame(2, "Game 1.exe");
        _ = A.CallTo(() => gameMonitor.GetRunningGames())
            .Returns([firstInstance, secondInstance]);

        var rule = new GameRule(
            gameMonitor,
            new GameRuleDto
            {
                Games = ["Game 1.exe"],
                SchemeGuid = Guid.NewGuid(),
            });
        rule.StartRuling();

        Assert.AreEqual(2, rule.TriggerCount);

        gameMonitor.GameTerminated +=
            Raise.With(new GameEventArgs(firstInstance));

        Assert.AreEqual(1, rule.TriggerCount);
    }

    [TestMethod]
    public void Dispose_StopsListeningForGameEvents()
    {
        var rule = CreateStartedRuleWithFilter("Game 1.exe");
        var game = CreateGame(1, "Game 1.exe");

        rule.Dispose();
        gameMonitor.GameStarted += Raise.With(new GameEventArgs(game));

        Assert.AreEqual(0, rule.TriggerCount);
    }

    [TestMethod]
    public void Description_WithoutGames_DescribesAnyGame()
    {
        var dto = new GameRuleDto();

        Assert.AreEqual("Game -> Any game", dto.GetDescription());
    }

    [TestMethod]
    public void Description_WithGames_ListsGames()
    {
        var dto = new GameRuleDto
        {
            Games = ["Game 1.exe", "game 2.exe"],
        };

        Assert.AreEqual(
            "Game -> Game 1.exe, game 2.exe",
            dto.GetDescription());
    }

    private GameRule CreateStartedRuleWithFilter(params string[] games)
    {
        _ = A.CallTo(() => gameMonitor.GetRunningGames())
            .Returns([]);

        var rule = new GameRule(
            gameMonitor,
            new GameRuleDto
            {
                Games = [.. games],
                SchemeGuid = Guid.NewGuid(),
            });
        rule.StartRuling();
        return rule;
    }
}
