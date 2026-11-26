namespace RuleManagement.Rules;

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
    private readonly HashSet<string> gameNames;
    private readonly Dictionary<GameIdentity, IGame> matchedGames = [];
    private readonly object syncRoot = new();

    public Guid SchemeGuid => Dto.SchemeGuid;

    public GameRule(
        IGameMonitor gameMonitor,
        GameRuleDto gameRuleDto)
        : base(gameRuleDto)
    {
        this.gameMonitor = gameMonitor;
        gameNames = new HashSet<string>(
            gameRuleDto.GetGameNames().Select(GetExecutableName),
            StringComparer.OrdinalIgnoreCase);
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
        var executableName = GetExecutableName(game.ExecutableName);
        if (string.IsNullOrWhiteSpace(executableName))
        {
            return false;
        }

        return gameNames.Count == 0
            || gameNames.Contains(executableName);
    }

    private static string GetExecutableName(string? executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
        {
            return string.Empty;
        }

        var trimmedName = executableName.Trim();
        var separatorIndex = Math.Max(
            trimmedName.LastIndexOf('\\'),
            trimmedName.LastIndexOf('/'));

        return separatorIndex >= 0
            ? trimmedName[(separatorIndex + 1)..].Trim()
            : trimmedName;
    }

    private static GameIdentity GetIdentity(IGame game) =>
        new(game.ProcessId, game.ExecutableName);

    public void Dispose() => StopRuling();
}
