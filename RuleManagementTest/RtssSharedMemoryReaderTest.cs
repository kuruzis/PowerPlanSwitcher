namespace RuleManagementTest;

using System.IO.MemoryMappedFiles;
using System.Text;
using GameManagement;

[TestClass]
public sealed class RtssSharedMemoryReaderTest
{
    private const int AppEntrySize = 268;
    private const int AppArrayOffset = 64;

    [TestMethod]
    public void ReadGames_ReturnsTrackedApplications()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive(
                "RTSS shared memory is only available on Windows.");
        }

        var mappingName =
            $"RTSSSharedMemoryV2Test_{Guid.NewGuid():N}";

        using var mapping = MemoryMappedFile.CreateNew(
            mappingName,
            4096);

        using (var view = mapping.CreateViewAccessor())
        {
            view.Write(0, unchecked((int)0x52545353)); // 'RTSS'
            view.Write(4, 0x00020000);
            view.Write(8, AppEntrySize);
            view.Write(12, AppArrayOffset);
            view.Write(16, 3);

            WriteAppEntry(
                view,
                AppArrayOffset,
                processId: 1234,
                executableName: "Game 1.exe",
                flags: 0x00000007);
            WriteAppEntry(
                view,
                AppArrayOffset + AppEntrySize,
                processId: 2345,
                executableName: "Launcher.exe",
                flags: 0);
            WriteAppEntry(
                view,
                AppArrayOffset + (2 * AppEntrySize),
                processId: 3456,
                executableName: @"C:\Games\Game 2.exe",
                flags: 0x0000000A);
        }

        var reader = new RtssSharedMemoryReader(mappingName);

        var games = reader.ReadGames();

        Assert.IsNotNull(games);
        CollectionAssert.AreEqual(
            new[] { 1234, 2345, 3456 },
            games.Select(game => game.ProcessId).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Game 1.exe", "Launcher.exe", "Game 2.exe" },
            games.Select(game => game.ExecutableName).ToArray());
    }

    [TestMethod]
    public void ReadGames_ReturnsNullWhenMappingDoesNotExist()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive(
                "RTSS shared memory is only available on Windows.");
        }

        var reader = new RtssSharedMemoryReader(
            $"RTSSSharedMemoryV2Test_{Guid.NewGuid():N}");

        Assert.IsNull(reader.ReadGames());
    }

    private static void WriteAppEntry(
        MemoryMappedViewAccessor view,
        int offset,
        int processId,
        string executableName,
        int flags)
    {
        view.Write(offset, processId);

        var executableNameBytes = new byte[260];
        var encodedName = Encoding.ASCII.GetBytes(executableName);
        Array.Copy(
            encodedName,
            executableNameBytes,
            Math.Min(encodedName.Length, executableNameBytes.Length - 1));
        view.WriteArray(offset + 4, executableNameBytes, 0, 260);

        view.Write(offset + 4 + 260, flags);
    }
}
