using System.Runtime.InteropServices;

namespace MurmurHash.UnitTests;

/// <summary>
/// Places data so that it ends exactly at an inaccessible (PROT_NONE) page, or with <c>guardBefore</c> starts
/// exactly after one: any read outside the span crashes the process instead of silently reading neighbouring
/// memory. Linux only.
/// </summary>
internal static unsafe partial class GuardedMemory
{
    public delegate void SpanAction(ReadOnlySpan<byte> data);

    private const int PageSize = 4096;
    private const int ProtNone = 0;
    private const int ProtRead = 1;
    private const int ProtWrite = 2;
    private const int MapPrivate = 0x02;
    private const int MapAnonymous = 0x20;

    public static bool IsSupported => OperatingSystem.IsLinux() && Environment.SystemPageSize == PageSize;

    public static void Run(ReadOnlySpan<byte> data, SpanAction action, bool guardBefore = false)
    {
        int dataPages = Math.Max(1, (data.Length + PageSize - 1) / PageSize);
        nuint total = (nuint)((dataPages + 1) * PageSize);
        nint mem = Mmap(0, total, ProtRead | ProtWrite, MapPrivate | MapAnonymous, -1, 0);
        if (mem == -1)
        {
            throw new InvalidOperationException("mmap failed");
        }

        try
        {
            byte* guard = guardBefore ? (byte*)mem : (byte*)mem + dataPages * PageSize;
            if (Mprotect((nint)guard, PageSize, ProtNone) != 0)
            {
                throw new InvalidOperationException("mprotect failed");
            }

            byte* start = guardBefore ? guard + PageSize : guard - data.Length;
            var span = new Span<byte>(start, data.Length);
            data.CopyTo(span);
            action(span);
        }
        finally
        {
            _ = Munmap(mem, total);
        }
    }

    [LibraryImport("libc", EntryPoint = "mmap")]
    private static partial nint Mmap(nint addr, nuint length, int prot, int flags, int fd, nint offset);

    [LibraryImport("libc", EntryPoint = "mprotect")]
    private static partial int Mprotect(nint addr, nuint length, int prot);

    [LibraryImport("libc", EntryPoint = "munmap")]
    private static partial int Munmap(nint addr, nuint length);
}
