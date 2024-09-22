using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public class XdpOpenURI(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalOpenURI Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    public async Task OpenURI(WindowId parentWindow, Uri uri, bool? writable = null, bool? ask = null, CancellationToken cancellationToken = default)
    {
        await using XdpRequest req = await XdpRequest.Create(dbusConnection);

        Dictionary<string, Variant> options = new()
        {
            ["handle_token"] = req.HandleToken
        };

        if (writable != null) options["writable"] = writable.Value;
        if (ask != null) options["ask"] = ask.Value;

        req.ValidatePath(await Wrapped.OpenURIAsync(parentWindow.Value, uri.ToString(), options));

        await req.Await(cancellationToken);
    }

    public async Task OpenFile(WindowId parentWindow, SafeFileHandle file, bool? writable = null, bool? ask = null, CancellationToken cancellationToken = default)
    {
        await using XdpRequest req = await XdpRequest.Create(dbusConnection);

        Dictionary<string, Variant> options = new()
        {
            ["handle_token"] = req.HandleToken
        };

        if (writable != null) options["writable"] = writable.Value;
        if (ask != null) options["ask"] = ask.Value;

        req.ValidatePath(await Wrapped.OpenFileAsync(parentWindow.Value, file, options));

        await req.Await(cancellationToken);
    }

    public async Task OpenFile(WindowId parentWindow, FileInfo file, bool? writable = null, bool? ask = null, CancellationToken cancellationToken = default)
    {
        using var stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.Delete);
        await OpenFile(parentWindow, stream.SafeFileHandle, writable: writable, ask: ask, cancellationToken: cancellationToken);
    }

    public async Task OpenDirectory(WindowId parentWindow, SafeFileHandle directory, CancellationToken cancellationToken = default)
    {
        await using XdpRequest req = await XdpRequest.Create(dbusConnection);

        Dictionary<string, Variant> options = new()
        {
            ["handle_token"] = req.HandleToken
        };

        req.ValidatePath(await Wrapped.OpenDirectoryAsync(parentWindow.Value, directory, options));

        await req.Await(cancellationToken);
    }

    public async Task OpenDirectory(WindowId parentWindow, DirectoryInfo directory, CancellationToken cancellationToken = default)
    {
        using SafeFileHandle dirFd = UnixNativeStuff.OpenDir(directory.FullName);

        await OpenDirectory(parentWindow, dirFd, cancellationToken);
    }
}