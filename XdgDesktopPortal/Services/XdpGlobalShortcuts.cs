using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public delegate void XdpShortcutStateChangedHandler(ulong timestamp);

public class XdpGlobalShortcuts
{
    private readonly Connection _connection;
    private readonly OrgFreedesktopPortalGlobalShortcuts _wrapped;

    private readonly SignalListener<(ObjectPath SessionHandle, string ShortcutId, ulong Timestamp)> _activated;
    private readonly SignalListener<(ObjectPath SessionHandle, string ShortcutId, ulong Timestamp)> _deactivated;
    private readonly SignalListener<(ObjectPath SessionHandle, (string, Dictionary<string, VariantValue>)[])> _shortcutsChanged;

    public XdpGlobalShortcuts(Connection dbusConnection)
    {
        _connection = dbusConnection;
        _wrapped = new OrgFreedesktopPortalGlobalShortcuts(dbusConnection, XdpUtils.Destination,
            XdpUtils.DesktopObject);

        _activated = new(invoker => _wrapped.WatchActivatedAsync((e, data) => invoker((data.SessionHandle, data.ShortcutId, data.Timestamp))));
        _deactivated = new(invoker => _wrapped.WatchDeactivatedAsync((e, data) => invoker((data.SessionHandle, data.ShortcutId, data.Timestamp))));
        _shortcutsChanged = new(invoker => _wrapped.WatchShortcutsChangedAsync((e, data) => invoker((data.SessionHandle, data.Shortcuts))));
    }

    public async Task<Session> CreateSession(CancellationToken token = default)
    {
        await using XdpRequest req = await XdpRequest.Create(_connection);
        XdpSession session = await XdpSession.Create(_connection);

        req.ValidatePath(await _wrapped.CreateSessionAsync(new Dictionary<string, Variant>
        {
            ["handle_token"] = req.HandleToken,
            ["session_handle_token"] = session.HandleToken
        }));

        session.ValidatePath((await req.Await(token))["session_handle"].GetString());

        return new Session(this, session);
    }

    public class Session : IAsyncDisposable
    {
        internal readonly XdpGlobalShortcuts _parent;
        private readonly XdpSession _session;

        public ObjectPath SessionHandle => _session.SessionPath;
        
        internal Session(XdpGlobalShortcuts parent, XdpSession session)
        {
            _parent = parent;
            _session = session;
        }

        public async Task<BoundShortcut[]> BindShortcuts(WindowId parentWindow, CancellationToken token = default,
            params BindableShortcut[] shortcuts)
        {
            await using XdpRequest req = await XdpRequest.Create(_parent._connection);

            req.ValidatePath(await _parent._wrapped.BindShortcutsAsync(
                _session.SessionPath,
                shortcuts.Select(x => x.ToDBus()).ToArray(),
                parentWindow.Value,
                new() { ["handle_token"] = req.HandleToken })
            );

            return (await req.Await(token))["shortcuts"]
                .GetArray<VariantValue>()
                .Select(x => BoundShortcut.FromDBus(this, x))
                .ToArray();
        }
        
        // TODO: ListShortcuts
        
        // TODO: ShortcutsChanged

        public async ValueTask DisposeAsync()
        {
            await _session.DisposeAsync();
        }
    }

    public class BindableShortcut
    {
        public required string Id { get; set; }
        public required string Description { get; set; }
        public string? PreferredTrigger { get; set; }

        internal (string, Dictionary<string, Variant>) ToDBus()
        {
            Dictionary<string, Variant> options = [];

            options["description"] = Description;
            if (PreferredTrigger != null) options["preferred_trigger"] = PreferredTrigger;
            
            return
            (
                Id,
                options
            );
        }
    }
    
    public class BoundShortcut
    {
        public string Id { get; }
        public string Description { get; }
        public string TriggerDescription { get; }

        private readonly EventMapper<Action<(ObjectPath SessionHandle, string ShortcutId, ulong Timestamp)>, XdpShortcutStateChangedHandler> _activated;
        public event XdpShortcutStateChangedHandler Activated
        {
            add => _activated.Add(value);
            remove => _activated.Remove(value);
        }
        
        private readonly EventMapper<Action<(ObjectPath SessionHandle, string ShortcutId, ulong Timestamp)>, XdpShortcutStateChangedHandler> _deactivated;
        public event XdpShortcutStateChangedHandler Deactivated
        {
            add => _deactivated.Add(value);
            remove => _deactivated.Remove(value);
        }
        
        private BoundShortcut(Session session, string id, string description, string triggerDescription)
        {
            Id = id;
            Description = description;
            TriggerDescription = triggerDescription;
            
            _activated = new(
                listeners => data =>
                {
                    if (data.SessionHandle != session.SessionHandle) return;
                    if (data.ShortcutId != Id) return;

                    foreach (var listener in listeners)
                        listener(data.Timestamp);
                },
                invoker => session._parent._activated.Fired += invoker,
                invoker => session._parent._activated.Fired -= invoker
            );
            
            _deactivated = new(
                listeners => data =>
                {
                    if (data.SessionHandle != session.SessionHandle) return;
                    if (data.ShortcutId != Id) return;

                    foreach (var listener in listeners)
                        listener(data.Timestamp);
                },
                invoker => session._parent._deactivated.Fired += invoker,
                invoker => session._parent._deactivated.Fired -= invoker
            );
        }

        internal static BoundShortcut FromDBus(Session session, VariantValue dbus)
        {
            string id = dbus.GetItem(0).GetString();
            string? desc = null;
            string? triggerDesc = null;

            foreach (var entry in dbus.GetItem(1).GetDictionary<string, VariantValue>())
            {
                if (entry.Key == "description") desc = entry.Value.GetString();
                else if (entry.Key == "trigger_description") triggerDesc = entry.Value.GetString();
            }

            if (desc is null || triggerDesc is null) throw new NotImplementedException(); //TODO: pick exception???

            return new BoundShortcut(session, id, desc, triggerDesc);
        }
    }
}