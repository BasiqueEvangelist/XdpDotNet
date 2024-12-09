using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

/// <summary>
/// <c>org.freedesktop.portal.Trash</c>: Portal for trashing files
/// </summary>
///
/// <remarks>
/// <para>This simple interface lets sandboxed applications send files to
/// the trashcan.</para>
/// 
/// <para>This documentation describes version 1 of this interface.</para>
/// </remarks>
/// 
/// <param name="dbusConnection">The DBus connection to use for this portal wrapper.</param>
/// <seealso href="https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Trash.html"/>
public class XdpTrash(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalTrash Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    /// <summary>
    /// Sends a file to the trashcan. Applications are allowed to trash a file if they can open it in r/w mode.
    /// </summary>
    /// <param name="file">file descriptor for the file to trash</param>
    /// <exception cref="InvalidOperationException">trashing file failed</exception>
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