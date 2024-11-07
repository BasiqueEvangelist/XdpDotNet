namespace XdgDesktopPortal;

internal class SignalListener<T>
{
    private readonly Func<Action<T>, ValueTask<IDisposable>> _watcher;

    public SignalListener(Func<Action<T>, ValueTask<IDisposable>> watcher)
    {
        _watcher = watcher;
    }
    
    private readonly List<Action<T>> _listeners = new();
    private IDisposable? _handle;
    
    public event Action<T> Fired
    {
        add
        {
            if (_listeners.Count == 0)
            {
                Task.Run(async () =>
                {
                    _handle = await _watcher(CallListeners);
                });
            }
            
            _listeners.Add(value);
        }

        remove
        {
            _listeners.Remove(value);

            if (_listeners.Count == 0)
            {
                _handle?.Dispose();
                _handle = null;
            }
        }
    }

    private void CallListeners(T data)
    {
        foreach (var listener in _listeners) listener(data);
    }
}