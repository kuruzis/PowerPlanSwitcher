namespace GameManagement;

public class GameEventArgs(IGame game) : EventArgs
{
    public IGame Game { get; } = game;
}
