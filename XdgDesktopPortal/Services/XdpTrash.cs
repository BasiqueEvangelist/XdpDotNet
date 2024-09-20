using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public class XdpTrash(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalTrash Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    public async Task TrashFile(SafeFileHandle file)
    {
        uint result = await Wrapped.TrashFileAsync(file);

        if (result != 1)
        {
            // todo: pick better exception.
            throw new InvalidOperationException("Trashing failed");
        }
    }
}