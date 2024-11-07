
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal;

internal class XdpSession : IAsyncDisposable
{
    public string HandleToken { get; }
    public ObjectPath SessionPath { get; }

    private readonly OrgFreedesktopPortalSession proxy;
    private readonly TaskCompletionSource<Dictionary<string, VariantValue>> closedFuture = new();
    private IDisposable? closedListener;

    private XdpSession(Connection connection, string handleToken)
    {
        string sender = connection.UniqueName!.TrimStart(':').Replace('.', '_');

        this.HandleToken = handleToken;
        this.SessionPath = $"/org/freedesktop/portal/desktop/session/{sender}/{handleToken}";

        this.proxy = new OrgFreedesktopPortalSession(connection, XdpUtils.Destination, SessionPath);
    }

    public static async Task<XdpSession> Create(Connection connection)
    {
        var req = new XdpSession(connection, XdpUtils.GenerateHandleToken());
        await req.Setup();
        return req;
    }

    private async Task Setup()
    {
        this.closedListener = await proxy.WatchClosedAsync(this.HandleClosed, true, ObserverFlags.EmitOnDispose);
    }

    private void HandleClosed(Exception? error, Dictionary<string, VariantValue> results)
    {
        if (closedFuture.Task.IsCompleted) return;

        this.closedListener = null;

        if (error != null)
        {
            closedFuture.SetException(error);
            return;
        }

        closedFuture.SetResult(results);
    }

    public void ValidatePath(ObjectPath actual)
    {
        if (SessionPath != actual)
            throw new InvalidOperationException($"Expected session path {SessionPath}, got {actual}");
    }

    public async Task<Dictionary<string, VariantValue>> AwaitClosure(CancellationToken token)
        => await closedFuture.Task.WaitAsync(token);

    public async ValueTask DisposeAsync()
    {
        if (closedListener == null) return;

        closedListener.Dispose();
        closedListener = null;
        await proxy.CloseAsync();
    }
}