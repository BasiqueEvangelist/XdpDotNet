using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public class XdpFileTransfer(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalFileTransfer _wrapped = new(dbusConnection, XdpUtils.DocumentsDestination, XdpUtils.DocumentsObject);

    private readonly List<Action<string>> transferClosedListeners = new();
    private IDisposable? transferClosedHandle;
    private event Action<string> TransferClosed
    {
        add
        {
            if (transferClosedListeners.Count == 0)
            {
                Task.Run(async () =>
                {
                    transferClosedHandle = await _wrapped.WatchTransferClosedAsync((exception, key) =>
                    {
                        foreach (var listener in transferClosedListeners)
                        {
                            listener(key);
                        }
                    });
                });
            }
            
            transferClosedListeners.Add(value);
        }

        remove
        {
            transferClosedListeners.Remove(value);

            if (transferClosedListeners.Count == 0)
            {
                transferClosedHandle?.Dispose();
                transferClosedHandle = null;
            }
        }
    } 
    
    public async Task<TransferSession> StartTransfer(bool? writable = null, bool? autoStop = null)
    {
        Dictionary<string, Variant> options = new();

        if (writable != null) options["writable"] = writable.Value;
        if (autoStop != null) options["autostop"] = autoStop.Value;

        return new TransferSession(this, await _wrapped.StartTransferAsync(options));
    }

    public async Task<string[]> RetrieveFiles(string key)
    {
        return await _wrapped.RetrieveFilesAsync(key, []);
    }
    
    public class TransferSession : IAsyncDisposable
    {
        private readonly XdpFileTransfer _parent;
        
        public string Key { get; }

        public bool IsClosed { get; private set; }

        private Action<string>? transferClosedHandler;
        private TaskCompletionSource closedTask = new();
        
        internal TransferSession(XdpFileTransfer parent, string key)
        {
            _parent = parent;
            Key = key;
            
            transferClosedHandler = OnClosed;
            parent.TransferClosed += transferClosedHandler;
        }

        public async Task AddFiles(params SafeFileHandle[] fds)
        {
            await _parent._wrapped.AddFilesAsync(Key, fds.ToArray(), []);
        }
        
        public async Task AddFiles(params FileInfo[] files)
        {
            SafeFileHandle?[] fds = new SafeFileHandle[files.Length];

            try
            {
                for (int i = 0; i < files.Length; i++)
                    fds[i] = File.OpenHandle(files[i].FullName, FileMode.Open, FileAccess.ReadWrite);

                await AddFiles(fds!);
            }
            finally
            {
                foreach (var handle in fds)
                    handle?.Close();
            }
        }

        public Task AwaitClosure() => closedTask.Task;

        private void OnClosed(string key)
        {
            if (Key != key) return;
            
            IsClosed = true;
            closedTask.SetResult();
            _parent.TransferClosed -= transferClosedHandler;
        }

        public async ValueTask DisposeAsync()
        {
            if (IsClosed) return;
            
            OnClosed(Key);
            await _parent._wrapped.StopTransferAsync(Key);
        }
    } 
}