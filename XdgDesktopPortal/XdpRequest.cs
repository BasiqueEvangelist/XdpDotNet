
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal;

internal class XdpRequest : IAsyncDisposable
{
    public string HandleToken { get; }
    public ObjectPath RequestPath { get; }

    private readonly OrgFreedesktopPortalRequest proxy;
    private readonly TaskCompletionSource<Dictionary<string, VariantValue>> future = new();
    private IDisposable? finishListener;

    private XdpRequest(Connection connection, string handleToken) {
        string sender = connection.UniqueName!.TrimStart(':').Replace('.', '_');

        this.HandleToken = handleToken;
        this.RequestPath = $"/org/freedesktop/portal/desktop/request/{sender}/{handleToken}";

        this.proxy = new OrgFreedesktopPortalRequest(connection, XdpUtils.Destination, RequestPath);
    }

    public static async Task<XdpRequest> Create(Connection connection) {
        static string GenerateHandleToken()
        {
            Span<char> token = stackalloc char[16];
            Random.Shared.GetItems("abcdefghijklmnopqrstuvwxyz0123456789", token);
            return $"xdp_dot_net_{token}";
        }

        var req = new XdpRequest(connection, GenerateHandleToken());
        await req.Setup();
        return req;
    }

    private async Task Setup()
    {
        this.finishListener = await proxy.WatchResponseAsync(this.HandleResponse, true, ObserverFlags.EmitOnDispose);
    }

    private void HandleResponse(Exception? error, (uint Response, Dictionary<string, VariantValue> Results) result) {
        if (future.Task.IsCompleted) return;

        this.finishListener = null;

        if (error != null)
        {
            future.SetException(error);
            return;
        }

        if (result.Response == 0) // Success, the request is carried out
        {
            future.SetResult(result.Results);
        }
        else if (result.Response == 1) // The user cancelled the interaction
        {
            future.SetException(new CancelledByUserException());
        }
        else if (result.Response == 2) // The user interaction was ended in some other way
        {
            future.SetException(new UserInteractionAbortedException());
        }
    }

    public void ValidatePath(ObjectPath actual) {
        if (RequestPath != actual)
            throw new InvalidOperationException($"Expected request path {RequestPath}, got {actual}");
    }

    public async Task<Dictionary<string, VariantValue>> Await(CancellationToken token)
    {
        using var reg = token.Register(() => future.TrySetCanceled(token));
        return await future.Task;
    }

    public async ValueTask DisposeAsync()
    {
        if (finishListener == null) return;

        finishListener.Dispose();
        finishListener = null;
        await proxy.CloseAsync();
    }
}