using System.Runtime.InteropServices;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

/// <summary><c>org.freedesktop.portal.Email</c>: Portal for sending email</summary>
///
/// <remarks>
/// <para>This simple portal lets sandboxed applications request to send an email,
/// optionally providing an address, subject, body and attachments.</para>
///
/// <para>This documentation describes version 4 of this interface.</para>
/// </remarks>
/// <param name="dbusConnection">The DBus connection to use for this portal wrapper.</param>
///
/// <seealso href="https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Email.html"/>
public class XdpEmail(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalEmail Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    /// <summary>
    /// Presents a window that lets the user compose an email.
    /// </summary>
    ///
    /// <remarks>
    /// <para>Note that the default email client for the host will need to support
    /// mailto: URIs following RFC 2368, with “cc”, “bcc”, “subject” and “body” query
    /// keys each corresponding to the email header of the same name, and with each
    /// attachment being passed as a file:// URI as a value in an “attachment” query key.</para>
    /// </remarks>
    /// 
    /// <param name="parentWindow">Identifier for the application window.</param>
    /// <param name="message">The email message to send.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
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

/// <summary>
/// A bundle of options describing an email message
/// </summary>
///
/// <seealso cref="XdpEmail.ComposeEmail"/>
public class EmailMessage
{
    /// <summary>
    /// Email addresses to send to. Must conform to the HTML5 definition of a
    /// <see href="https://html.spec.whatwg.org/multipage/input.html#valid-e-mail-address">valid email address</see>.
    /// </summary>
    public List<string>? Addresses { get; set; } = [];

    /// <summary>
    /// Email addresses to cc.
    /// </summary>
    public List<string>? Cc { get; set; } = [];

    /// <summary>
    /// Email addresses to bcc.
    /// </summary>
    public List<string>? Bcc { get; set; } = [];

    /// <summary>
    /// The subject for the email.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The body for the email.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// File descriptors for files to attach.
    /// </summary>
    public List<SafeHandle>? Attachments { get; set; } = [];

    /// <summary>
    /// A token that can be used to activate the chosen application
    /// </summary>
    ///
    /// <seealso href="https://wayland.app/protocols/xdg-activation-v1" />
    public string? ActivationToken { get; set; }

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

        if (ActivationToken != null) options["activation_token"] = ActivationToken;
    }
}