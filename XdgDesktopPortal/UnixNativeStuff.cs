using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace XdgDesktopPortal;

internal static partial class UnixNativeStuff {
    private const int O_DIRECTORY = 65536;

    public static UnixPipe CreatePipe() {
        int[] files = new int[2];
        if (pipe2(files, 0) != 0) throw new InvalidOperationException("pipe2 exploded :uhoh:");

        return new UnixPipe(new SafeFileHandle(files[0], true), new SafeFileHandle(files[1], true));
    }

    public static SafeFileHandle OpenDir(string path) => new SafeFileHandle(open(path, O_DIRECTORY), true);

    [LibraryImport("libc")]
    private static partial int pipe2(Span<int> pipefd, int flags);

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int open(string pathname, int flags);

    internal record struct UnixPipe(SafeFileHandle ReadEnd, SafeFileHandle WriteEnd) : IDisposable
    {
        public void Dispose()
        {
            ReadEnd.Dispose();
            WriteEnd.Dispose();
        }
    }
}