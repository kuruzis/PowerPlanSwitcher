namespace GameManagement;

using Serilog;

public class GameMonitor : IGameMonitor, IDisposable
{
    private readonly record struct GameIdentity(
        int ProcessId,
        string ExecutableName);

    private sealed class GameIdentityComparer : IEqualityComparer<GameIdentity>
    {
        public bool Equals(GameIdentity x, GameIdentity y) =>
            x.ProcessId == y.ProcessId
            && string.Equals(
                x.ExecutableName,
                y.ExecutableName,
                StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(GameIdentity obj) =>
            HashCode.Combine(
                obj.ProcessId,
                StringComparer.OrdinalIgnoreCase.GetHashCode(
                    obj.ExecutableName));
    }

    private static readonly GameIdentityComparer IdentityComparer = new();

    private readonly RtssSharedMemoryReader reader;
    private readonly WindowMessageTimer.Timer timer;
    private readonly object syncRoot = new();

    private Dictionary<GameIdentity, IGame> previousGames =
        new(IdentityComparer);
    private bool? rtssAvailable;
    private bool disposedValue;

    public event EventHandler<GameEventArgs>? GameStarted;
    public event EventHandler<GameEventArgs>? GameTerminated;

    public GameMonitor(TimeSpan pollingInterval)
        : this(pollingInterval, new RtssSharedMemoryReader())
    {
    }

    internal GameMonitor(
        TimeSpan pollingInterval,
        RtssSharedMemoryReader reader)
    {
        this.reader = reader;
        timer = new(pollingInterval);
        timer.Tick += Timer_Tick;
    }

    private void Timer_Tick()
    {
        List<IGame> startedGames;
        List<IGame> terminatedGames;

        lock (syncRoot)
        {
            var currentGames = ReadGames();
            var currentByIdentity = currentGames.ToDictionary(
                GetIdentity,
                IdentityComparer);

            startedGames = [.. currentByIdentity.Values
                .Where(game => !previousGames.ContainsKey(GetIdentity(game)))];
            terminatedGames = [.. previousGames.Values
                .Where(game => !currentByIdentity.ContainsKey(GetIdentity(game)))];
            previousGames = currentByIdentity;
        }

        foreach (var game in terminatedGames)
        {
            LogGameTerminated(game);
            GameTerminated?.Invoke(this, new GameEventArgs(game));
        }

        foreach (var game in startedGames)
        {
            LogGameDetected(game);
            GameStarted?.Invoke(this, new GameEventArgs(game));
        }
    }

    private static void LogGameDetected(IGame game) =>
        Log.ForContext("EventType", "Game.Detected")
            .Information(
                "Game detected: {ExecutableName} (PID {ProcessId})",
                game.ExecutableName,
                game.ProcessId);

    private static void LogGameTerminated(IGame game) =>
        Log.ForContext("EventType", "Game.Terminated")
            .Debug(
                "Game terminated: {ExecutableName} (PID {ProcessId})",
                game.ExecutableName,
                game.ProcessId);

    private IReadOnlyList<IGame> ReadGames()
    {
        IReadOnlyList<IGame>? games;

        try
        {
            games = reader.ReadGames();
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Failed to read game information from RTSS shared memory");
            games = null;
        }

        if (games is null)
        {
            SetRtssAvailability(false);
            return [];
        }

        SetRtssAvailability(true);
        return games;
    }

    private void SetRtssAvailability(bool available)
    {
        if (rtssAvailable == available)
        {
            return;
        }

        rtssAvailable = available;

        if (available)
        {
            Log.ForContext("EventType", "Game.MonitorAvailable")
                .Information(
                    "RTSS shared memory detected; game detection is active");
        }
        else
        {
            Log.ForContext("EventType", "Game.MonitorUnavailable")
                .Warning(
                    "RTSS shared memory not available; game rules will not " +
                    "trigger until RivaTuner Statistics Server is running");
        }
    }

    private static GameIdentity GetIdentity(IGame game) =>
        new(game.ProcessId, game.ExecutableName);

    public IEnumerable<IGame> GetRunningGames()
    {
        lock (syncRoot)
        {
            return [.. ReadGames()];
        }
    }

    public void StartMonitoring()
    {
        IReadOnlyList<IGame> runningGames;

        lock (syncRoot)
        {
            runningGames = ReadGames();
            previousGames = runningGames
                .ToDictionary(GetIdentity, IdentityComparer);
        }

        foreach (var game in runningGames)
        {
            LogGameDetected(game);
        }

        timer.Start();
        Log.ForContext("EventType", "Game.MonitoringStarted")
            .Information("Game monitoring started");
    }

    public void StopMonitoring()
    {
        timer.Stop();
        Log.ForContext("EventType", "Game.MonitoringStopped")
            .Information("Game monitoring stopped");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposedValue)
        {
            return;
        }

        if (disposing)
        {
            StopMonitoring();
            timer.Dispose();
        }

        disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
