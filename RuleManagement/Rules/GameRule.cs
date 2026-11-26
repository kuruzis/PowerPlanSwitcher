namespace RuleManagement.Rules;

using DotNet.Globbing;
using GameManagement;
using RuleManagement.Dto;

public sealed class GameRule
    : Rule<GameRuleDto>,
    IRule<GameRuleDto>,
    IDisposable
{
    private readonly record struct GameIdentity(
        int ProcessId,
        string ExecutableName);

    private readonly IGameMonitor gameMonitor;
    private readonly Glob[] whitelist;
    private readonly Glob[] blacklist;
    private readonly Dictionary<GameIdentity, IGame> matchedGames = [];
    private readonly object syncRoot = new();

    public Guid SchemeGuid => Dto.SchemeGuid;

    public GameRule(
        IGameMonitor gameMonitor,
        GameRuleDto gameRuleDto)
        : base(gameRuleDto)
    {
        this.gameMonitor = gameMonitor;
        whitelist = [.. gameRuleDto.GetGameNames().Select(ParseGlob)];
        blacklist = [.. gameRuleDto.GetBlacklistNames().Select(ParseGlob)];
    }

    public override void StartRuling()
    {
        gameMonitor.GameStarted += GameMonitor_GameStarted;
        gameMonitor.GameTerminated += GameMonitor_GameTerminated;

        lock (syncRoot)
        {
            matchedGames.Clear();
            TriggerCount = 0;

            foreach (var game in gameMonitor.GetRunningGames())
            {
                if (!CheckRule(game))
                {
                    continue;
                }

                if (!matchedGames.TryAdd(GetIdentity(game), game))
                {
                    continue;
                }

                TriggerCount++;
            }
        }
    }

    public override void StopRuling()
    {
        gameMonitor.GameStarted -= GameMonitor_GameStarted;
        gameMonitor.GameTerminated -= GameMonitor_GameTerminated;
    }

    private void GameMonitor_GameStarted(
        object? sender,
        GameEventArgs e)
    {
        lock (syncRoot)
        {
            if (!CheckRule(e.Game))
            {
                return;
            }

            if (!matchedGames.TryAdd(GetIdentity(e.Game), e.Game))
            {
                return;
            }

            TriggerCount++;
        }
    }

    private void GameMonitor_GameTerminated(
        object? sender,
        GameEventArgs e)
    {
        lock (syncRoot)
        {
            if (!CheckRule(e.Game))
            {
                return;
            }

            if (!matchedGames.Remove(GetIdentity(e.Game)))
            {
                return;
            }

            TriggerCount = Math.Max(TriggerCount - 1, 0);
        }
    }

    private bool CheckRule(IGame game)
    {
        var executableName = GameExecutableName.Normalize(
            game.ExecutableName);
        if (string.IsNullOrWhiteSpace(executableName))
        {
            return false;
        }

        if (blacklist.Length > 0
            && blacklist.Any(glob => glob.IsMatch(executableName)))
        {
            return false;
        }

        return whitelist.Length == 0
            || whitelist.Any(glob => glob.IsMatch(executableName));
    }

    private static Glob ParseGlob(string pattern) =>
        Glob.Parse(
            pattern,
            new GlobOptions
            {
                Evaluation = { CaseInsensitive = true }
            });

    private static GameIdentity GetIdentity(IGame game) =>
        new(game.ProcessId, game.ExecutableName);

    public void Dispose() => StopRuling();
}
