using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public class XdpAccount(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalAccount Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

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

public record XdgAccountInformation(string Id, string Name, Uri Image)
{
    internal static XdgAccountInformation FromResponse(Dictionary<string, VariantValue> response)
     => new XdgAccountInformation(
        response["id"].GetString(),
        response["name"].GetString(),
        new Uri(response["image"].GetString())
    );
}