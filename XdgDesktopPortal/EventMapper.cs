namespace XdgDesktopPortal;

internal class EventMapper<TFrom, TTo>
{
    private readonly Action<TFrom> _adder;
    private readonly Action<TFrom> _remover;
    private readonly List<TTo> _listeners = new();
    private readonly TFrom _mapper; 

    public EventMapper(Func<List<TTo>, TFrom> invokerFactory, Action<TFrom> adder, Action<TFrom> remover)
    {
        _adder = adder;
        _remover = remover;
        _mapper = invokerFactory(this._listeners);
    }

    public void Add(TTo listener) 
    {
        if (_listeners.Count == 0)
        {
            _adder(_mapper);
        }
        
        _listeners.Add(listener);
    }

    public void Remove(TTo listener)
    {
        _listeners.Remove(listener);

        if (_listeners.Count == 0)
        {
            _remover(_mapper);
        }
    }
}