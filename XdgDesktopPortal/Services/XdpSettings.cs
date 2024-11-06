using System.Collections.Concurrent;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public record struct XdpSettingChangedEvent(string Namespace, string Key, VariantValue NewValue);

public class XdpSettings(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalSettings Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    internal volatile Task<IReadOnlyDictionary<string, IReadOnlyDictionary<string, VariantValue>>>? allValues;
    private object allValuesLock = new();

    public event EventHandler<XdpSettingChangedEvent>? SettingChanged;
    
    private async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<string, VariantValue>>> Prepare()
    {
        ConcurrentDictionary<string, IReadOnlyDictionary<string, VariantValue>> settings = new();
        
        // TODO: actually dispose this
        _ = Wrapped.WatchSettingChangedAsync((exception, change) =>
        {
            if (exception != null) return;

            var target = (ConcurrentDictionary<string, VariantValue>)settings.GetOrAdd(change.Namespace,
                _ => new ConcurrentDictionary<string, VariantValue>());
            
            target[change.Key] = change.Value;
            SettingChanged?.Invoke(this, new XdpSettingChangedEvent(change.Namespace, change.Key, change.Value));
        });
        
        var orig = await Wrapped.ReadAllAsync([]);

        foreach (var nsEntry in orig)
        {
            var target = (ConcurrentDictionary<string, VariantValue>)settings.GetOrAdd(nsEntry.Key,
                _ => new ConcurrentDictionary<string, VariantValue>());

            foreach (var keyEntry in nsEntry.Value)
            {
                target[keyEntry.Key] = keyEntry.Value;
            }
        }

        return settings;
    }
    
    public Task<IReadOnlyDictionary<string, IReadOnlyDictionary<string, VariantValue>>> GetAll()
    {
        if (allValues == null)
        {
            lock (allValuesLock)
            {
                if (allValues == null)
                {
                    allValues = Prepare();
                }
            }
        }

        return allValues;
    }

    public class Setting<T>
    {
        private readonly XdpSettings parent;
        private readonly string ns;
        private readonly string key;
        private readonly Func<VariantValue, T> reader;

        private readonly List<Action<T>> changedListeners = new();

        internal Setting(XdpSettings parent, string ns, string key, Func<VariantValue, T> reader)
        {
            this.parent = parent;
            this.ns = ns;
            this.key = key;
            this.reader = reader;
        }

        public async ValueTask<T?> Get()
        {
            var values = await parent.GetAll();
            VariantValue raw = values.GetValueOrDefault(ns)?.GetValueOrDefault(key) ?? default;

            if (raw.Type == VariantValueType.Invalid) return default;
            
            return reader(raw);
        }
        
        private void FireChangedEvent(object? sender, XdpSettingChangedEvent ev)
        {
            if (ev.Namespace != ns || ev.Key != key) return;
            
            foreach (var listener in changedListeners)
            {
                listener(reader(ev.NewValue));
            }
        }
        
        public event Action<T> Changed
        {
            add
            {
                if (changedListeners.Count == 0) parent.SettingChanged += FireChangedEvent;
                
                changedListeners.Add(value);
            }

            remove
            {
                changedListeners.Remove(value);

                if (changedListeners.Count == 0) parent.SettingChanged -= FireChangedEvent;
            }
        } 
    }
}