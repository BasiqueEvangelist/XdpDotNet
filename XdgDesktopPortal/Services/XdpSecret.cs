using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public class XdpSecret(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalSecret Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    public async Task RetrieveSecret(SafeFileHandle outputFd, CancellationToken token = default)
    {
        await using XdpRequest req = await XdpRequest.Create(dbusConnection);

        Dictionary<string, Variant> options = new()
        {
            ["handle_token"] = req.HandleToken
        };

        req.ValidatePath(await Wrapped.RetrieveSecretAsync(outputFd, options));

        await req.Await(token);

        token.ThrowIfCancellationRequested();
    }

    public async Task<byte[]> RetrieveSecret(CancellationToken token = default)
    {
        using var pipe = UnixNativeStuff.CreatePipe();

        await RetrieveSecret(pipe.WriteEnd, token);

        using var stream = new FileStream(pipe.ReadEnd, FileAccess.Read);
        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms);

        return ms.ToArray();
    }
}