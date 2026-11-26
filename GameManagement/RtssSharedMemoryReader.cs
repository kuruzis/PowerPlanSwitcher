namespace GameManagement;

using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Reads the list of applications currently tracked by RivaTuner Statistics
/// Server through its shared memory interface.
/// </summary>
public sealed class RtssSharedMemoryReader
{
    public const string DefaultMappingName = "RTSSSharedMemoryV2";

    private const uint FileMapRead = 0x0004;
    private const uint RtssSignature = 0x52545353; // 'RTSS' (MSVC multi-character constant)
    private const uint MinimumRtssVersion = 0x00020000;
    private const int RtssHeaderSize = 40;
    private const int MaxPath = 260;
    private const int MinimumAppEntrySize = 4 + MaxPath + 4;
    private const int MaximumAppArraySize = 4096;

    private readonly string mappingName;

    public RtssSharedMemoryReader(string mappingName = DefaultMappingName) =>
        this.mappingName = mappingName;

    /// <summary>
    /// Reads the applications from the RTSS shared memory.
    /// </summary>
    /// <returns>
    /// The running applications, or <see langword="null"/> when the shared
    /// memory is not available or does not contain valid RTSS data.
    /// </returns>
    public IReadOnlyList<IGame>? ReadGames()
    {
        var mappingHandle = NativeMethods.OpenFileMappingW(
            FileMapRead,
            false,
            mappingName);
        if (mappingHandle == 0)
        {
            return null;
        }

        try
        {
            var view = NativeMethods.MapViewOfFile(
                mappingHandle,
                FileMapRead,
                0,
                0,
                0);
            if (view == 0)
            {
                return null;
            }

            try
            {
                return ReadGames(view);
            }
            finally
            {
                _ = NativeMethods.UnmapViewOfFile(view);
            }
        }
        finally
        {
            _ = NativeMethods.CloseHandle(mappingHandle);
        }
    }

    private static IReadOnlyList<IGame>? ReadGames(nint view)
    {
        var signature = (uint)Marshal.ReadInt32(view, 0);
        var version = (uint)Marshal.ReadInt32(view, 4);
        if (signature != RtssSignature || version < MinimumRtssVersion)
        {
            return null;
        }

        var appEntrySize = Marshal.ReadInt32(view, 8);
        var appArrayOffset = Marshal.ReadInt32(view, 12);
        var appArraySize = Marshal.ReadInt32(view, 16);

        if (appEntrySize < MinimumAppEntrySize
            || appArrayOffset < RtssHeaderSize
            || appArraySize < 0
            || appArraySize > MaximumAppArraySize)
        {
            return null;
        }

        var viewSize = GetViewSize(view);
        var appArrayEnd = (long)appArrayOffset
            + ((long)appEntrySize * appArraySize);
        if (viewSize < RtssHeaderSize || appArrayEnd > viewSize)
        {
            return null;
        }

        var games = new List<IGame>(Math.Min(appArraySize, 256));
        var processIds = new HashSet<int>();

        for (var index = 0; index < appArraySize; index++)
        {
            var entry = view + appArrayOffset + (index * appEntrySize);
            var processId = Marshal.ReadInt32(entry, 0);
            if (processId <= 0 || !processIds.Add(processId))
            {
                continue;
            }

            // RTSS only creates entries for applications it tracks. Some
            // entries do not report an API-usage flag until the first frame,
            // so do not require it. Users can filter unwanted apps through
            // the rule's executable-name list.
            var executableName = ReadExecutableName(entry + 4);
            if (string.IsNullOrWhiteSpace(executableName))
            {
                continue;
            }

            games.Add(new Game(processId, executableName));
        }

        return games;
    }

    private static string ReadExecutableName(nint address)
    {
        var buffer = new byte[MaxPath];
        Marshal.Copy(address, buffer, 0, buffer.Length);

        var length = Array.IndexOf(buffer, (byte)0);
        if (length < 0)
        {
            length = buffer.Length;
        }

        return GetExecutableName(
            Encoding.UTF8.GetString(buffer, 0, length));
    }

    private static string GetExecutableName(string name)
    {
        var trimmedName = name.Trim();
        var separatorIndex = Math.Max(
            trimmedName.LastIndexOf('\\'),
            trimmedName.LastIndexOf('/'));

        return separatorIndex >= 0
            ? trimmedName[(separatorIndex + 1)..].Trim()
            : trimmedName;
    }

    private static long GetViewSize(nint view)
    {
        var result = NativeMethods.VirtualQuery(
            view,
            out var memoryInfo,
            (nuint)Marshal.SizeOf<NativeMethods.MemoryBasicInformation>());

        return result == 0 ? 0 : (long)memoryInfo.RegionSize;
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct MemoryBasicInformation
        {
            public nint BaseAddress;
            public nint AllocationBase;
            public uint AllocationProtect;
            public nuint RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [DllImport(
            "kernel32.dll",
            EntryPoint = "OpenFileMappingW",
            ExactSpelling = true,
            SetLastError = true,
            CharSet = CharSet.Unicode)]
        internal static extern nint OpenFileMappingW(
            uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern nint MapViewOfFile(
            nint hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            nuint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnmapViewOfFile(nint lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(nint hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern nuint VirtualQuery(
            nint lpAddress,
            out MemoryBasicInformation lpBuffer,
            nuint dwLength);
    }
}
