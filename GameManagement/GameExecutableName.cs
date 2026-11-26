namespace GameManagement;

public static class GameExecutableName
{
    /// <summary>
    /// Returns the executable file name without its directory. RTSS may report
    /// either a bare file name or a full path in its shared memory.
    /// </summary>
    public static string Normalize(string? executableName)
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
}
