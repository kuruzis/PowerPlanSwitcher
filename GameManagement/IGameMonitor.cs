namespace GameManagement;

public interface IGameMonitor
{
    public event EventHandler<GameEventArgs>? GameStarted;
    public event EventHandler<GameEventArgs>? GameTerminated;

    public IEnumerable<IGame> GetRunningGames();
    public void StartMonitoring();
    public void StopMonitoring();
}
