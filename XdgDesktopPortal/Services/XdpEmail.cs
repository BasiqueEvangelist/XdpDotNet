using System.Runtime.InteropServices;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public class XdpEmail(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalEmail Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    public async Task ComposeEmail(WindowId parentWindow, EmailMessage message, CancellationToken cancellationToken = default)
    {
        await using var req = await XdpRequest.Create(dbusConnection);

        Dictionary<string, Variant> options = new()
        {
            ["handle_token"] = req.HandleToken
        };

        message.WriteToOptions(options);

        req.ValidatePath(await Wrapped.ComposeEmailAsync(parentWindow.Value, options));

        await req.Await(cancellationToken);
    }
}

public class EmailMessage
{
    public List<string>? Addresses { get; set; } = [];

    public List<string>? Cc { get; set; } = [];

    public List<string>? Bcc { get; set; } = [];

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public List<SafeHandle>? Attachments { get; set; } = [];

    // public string? ActivationToken { get; set; }

    internal void WriteToOptions(Dictionary<string, Variant> options)
    {
        if (Addresses?.Count > 0)
        {
            if (Addresses.Count == 0)
            {
                options["address"] = Addresses[0];
            }
            else
            {
                options["addresses"] = new Tmds.DBus.Protocol.Array<string>(Addresses);
            }
        }

        if (Cc?.Count > 0) options["cc"] = new Tmds.DBus.Protocol.Array<string>(Cc);

        if (Bcc?.Count > 0) options["bcc"] = new Tmds.DBus.Protocol.Array<string>(Bcc);

        if (Subject != null) options["subject"] = Subject;

        if (Body != null) options["body"] = Body;

        if (Attachments?.Count > 0)
        {
            options["attachment_fds"] = new Tmds.DBus.Protocol.Array<SafeHandle>(Attachments);
        }
    }
}