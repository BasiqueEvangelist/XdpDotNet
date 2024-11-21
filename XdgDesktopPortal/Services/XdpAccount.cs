using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

/// <summary><c>org.freedesktop.portal.Account</c>: Portal for obtaining information about the user</summary>
///
/// <remarks>
/// <para>This simple interface lets sandboxed applications query basic
/// information about the user, like their name and avatar photo.</para>
///
/// <para>This documentation describes version 1 of this interface.</para>
/// </remarks>
/// <param name="dbusConnection">The DBus connection to use for this portal wrapper.</param>
///
/// <seealso href="https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Account.html"/>
public class XdpAccount(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalAccount Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    // TODO: add version getter
    
    /// <summary>
    /// Gets information about the user.
    /// </summary>
    /// <param name="window">Identifier for the window</param>
    /// <param name="reason">A string that can be shown in the dialog to explain
    /// why the information is needed. This should be a complete sentence that explains
    /// what the application will do with the returned information, for example: “Allows your
    /// personal information to be included with recipes you share with your friends”.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>The user's data</returns>
    public async Task<XdgAccountInformation> GetUserInformation(WindowId window, string? reason = null, CancellationToken cancellationToken = default)
    {
        await using XdpRequest req = await XdpRequest.Create(dbusConnection);

        Dictionary<string, Variant> options = new()
        {
            ["handle_token"] = req.HandleToken
        };

        if (reason != null) options["reason"] = reason;

        req.ValidatePath(await Wrapped.GetUserInformationAsync(window.Value, options));

        var ret = await req.Await(cancellationToken);

        return XdgAccountInformation.FromResponse(ret);
    }
}

/// <param name="Id">The user's string id</param>
/// <param name="Name">The user's real name</param>
/// <param name="Image">The URI of an image file for the user’s avatar photo</param>
public record XdgAccountInformation(string Id, string Name, Uri Image)
{
    internal static XdgAccountInformation FromResponse(Dictionary<string, VariantValue> response)
     => new XdgAccountInformation(
        response["id"].GetString(),
        response["name"].GetString(),
        new Uri(response["image"].GetString())
    );
}