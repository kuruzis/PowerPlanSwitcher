namespace GameManagement;

public sealed record Game(int ProcessId, string ExecutableName) : IGame;
