using System.Drawing;
using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public class XdpScreenshot(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalScreenshot Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    public async Task<Uri> Screenshot(WindowId parentWindow, bool? modal = null, bool? interactive = null, CancellationToken cancellationToken = default)
    {
        await using XdpRequest req = await XdpRequest.Create(dbusConnection);

        Dictionary<string, Variant> options = new()
        {
            ["handle_token"] = req.HandleToken
        };

        if (modal != null) options["modal"] = modal.Value;
        if (interactive != null) options["interactive"] = interactive.Value;

        req.ValidatePath(await Wrapped.ScreenshotAsync(parentWindow.Value, options));

        var res = await req.Await(cancellationToken);
        return new Uri(res["uri"].GetString());
    }

    public async Task<(double Red, double Green, double Blue)> PickColor(WindowId parentWindow, CancellationToken cancellationToken = default)
    {
        await using XdpRequest req = await XdpRequest.Create(dbusConnection);

        Dictionary<string, Variant> options = new()
        {
            ["handle_token"] = req.HandleToken
        };

        req.ValidatePath(await Wrapped.PickColorAsync(parentWindow.Value, options));

        var res = await req.Await(cancellationToken);

        double r = res["color"].GetItem(0).GetDouble();
        double g = res["color"].GetItem(1).GetDouble();
        double b = res["color"].GetItem(2).GetDouble();

        return (r, g, b);
    }
}