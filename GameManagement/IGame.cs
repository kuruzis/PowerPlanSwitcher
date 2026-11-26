namespace GameManagement;

public interface IGame
{
    public int ProcessId { get; }
    public string ExecutableName { get; }
}
