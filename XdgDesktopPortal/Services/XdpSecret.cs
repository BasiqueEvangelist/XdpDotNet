using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

/// <summary><c>org.freedesktop.portal.Secret</c>: Portal for retrieving application secret</summary>
/// 
/// <remarks>
/// <para>The Secret portal allows sandboxed applications to retrieve a
/// per-application secret.  The secret can then be used for
/// encrypting confidential data inside the sandbox.</para>
/// 
/// <para>This documentation describes version 1 of this interface.</para>
/// </remarks>
///
/// <param name="dbusConnection">The DBus connection to use for this portal wrapper.</param>
/// <seealso href="https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Secret.html"/>
public class XdpSecret(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalSecret Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    // TODO: figure out what to do about the `token` parameter.
    /// <summary>
    /// Retrieves a master secret for a sandboxed application.
    /// </summary>
    ///
    /// <remarks>
    /// <para>The master secret is unique per application and does not change as long as the application is installed
    /// (once it has been created). In a typical backend implementation, it is stored in the user's keyring, under the
    /// application ID as a key.</para>
    ///
    /// <para>While the master secret can be used for encrypting any confidential data in the sandbox, the format is
    /// opaque to the application. In particular, the length of the secret might not be sufficient for the use with
    /// certain encryption algorithm. In that case, the application is supposed to expand it using a KDF algorithm.</para>
    /// </remarks>
    /// 
    /// <param name="outputFd">Writable file descriptor for transporting the secret</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    public async Task RetrieveSecret(SafeFileHandle outputFd, CancellationToken cancellationToken = default)
    {
        await using XdpRequest req = await XdpRequest.Create(dbusConnection);

        Dictionary<string, Variant> options = new()
        {
            ["handle_token"] = req.HandleToken
        };

        req.ValidatePath(await Wrapped.RetrieveSecretAsync(outputFd, options));

        await req.Await(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Retrieves a master secret for a sandboxed application.
    /// </summary>
    ///
    /// <remarks>
    /// <para>The master secret is unique per application and does not change as long as the application is installed
    /// (once it has been created). In a typical backend implementation, it is stored in the user's keyring, under the
    /// application ID as a key.</para>
    ///
    /// <para>While the master secret can be used for encrypting any confidential data in the sandbox, the format is
    /// opaque to the application. In particular, the length of the secret might not be sufficient for the use with
    /// certain encryption algorithm. In that case, the application is supposed to expand it using a KDF algorithm.</para>
    /// </remarks>
    /// 
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>this application's master secret</returns>
    public async Task<byte[]> RetrieveSecret(CancellationToken cancellationToken = default)
    {
        using var pipe = UnixNativeStuff.CreatePipe();

        await RetrieveSecret(pipe.WriteEnd, cancellationToken);

        using var stream = new FileStream(pipe.ReadEnd, FileAccess.Read);
        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms, cancellationToken);

        return ms.ToArray();
    }
}