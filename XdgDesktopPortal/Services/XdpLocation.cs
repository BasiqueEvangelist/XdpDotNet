using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public class XdpLocation
{
    private readonly Connection _connection;
    private readonly OrgFreedesktopPortalLocation _wrapped;

    private readonly SignalListener<(ObjectPath SessionHandle, Dictionary<string, VariantValue> Location)> _locationUpdated;

    public XdpLocation(Connection dbusConnection)
    {
        _connection = dbusConnection;
        _wrapped = new OrgFreedesktopPortalLocation(dbusConnection, XdpUtils.Destination,
            XdpUtils.DesktopObject);
        
        _locationUpdated = new(invoker => _wrapped.WatchLocationUpdatedAsync((e, data) => invoker((data.SessionHandle, data.Location))));
    }
    
    public async Task<Session> CreateSession(uint distanceThreshold = 0, uint timeThreshold = 0,
        Accuracy accuracy = Accuracy.Exact)
    {
        XdpSession session = await XdpSession.Create(_connection);

        var dict = new Dictionary<string, Variant>
        {
            ["session_handle_token"] = session.HandleToken,
            ["distance-threshold"] = distanceThreshold,
            ["time-threshold"] = timeThreshold,
            ["accuracy"] = (uint)accuracy
        };
        
        session.ValidatePath(await _wrapped.CreateSessionAsync(dict));

        return new Session(this, session);
    }

    public class Session : IAsyncDisposable
    {
        internal readonly XdpLocation _parent;
        private readonly XdpSession _session;
        
        private readonly EventMapper<Action<(ObjectPath SessionHandle, Dictionary<string, VariantValue> Location)>, Action<Location>> _locationUpdated;
        public event Action<Location> LocationUpdated
        {
            add => _locationUpdated.Add(value);
            remove => _locationUpdated.Remove(value);
        }

        public ObjectPath SessionHandle => _session.SessionPath;

        internal Session(XdpLocation parent, XdpSession session)
        {
            _parent = parent;
            _session = session;
            
            _locationUpdated = new(
                listeners => data =>
                {
                    if (data.SessionHandle != SessionHandle) return;

                    var location = Location.FromDBus(data.Location);
                    
                    foreach (var listener in listeners)
                        listener(location);
                },
                invoker => _parent._locationUpdated.Fired += invoker,
                invoker => _parent._locationUpdated.Fired -= invoker
            );
        }

        public async Task Start(WindowId parentWindow, CancellationToken token = default)
        {
            await using XdpRequest req = await XdpRequest.Create(_parent._connection);

            req.ValidatePath(await _parent._wrapped.StartAsync(
                _session.SessionPath,
                parentWindow.Value,
                new() { ["handle_token"] = req.HandleToken })
            );

            await req.Await(token);
        }
        
        public async ValueTask DisposeAsync()
        {
            await _session.DisposeAsync();
        }
    }

    public enum Accuracy : uint
    {
        None = 0,
        Country = 1,
        City = 2,
        Neighborhood = 3,
        Street = 4,
        Exact = 5
    }

    public record struct Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Accuracy { get; set; }
        public double Speed { get; set; }
        public double Heading { get; set; }
        public DateTime Timestamp { get; set; }

        internal static Location FromDBus(Dictionary<string, VariantValue> dbus)
        {
            DateTime timestamp = DateTime.UnixEpoch
                                 + TimeSpan.FromSeconds(dbus["Timestamp"].GetItem(0).GetUInt64())
                                 + TimeSpan.FromMicroseconds(dbus["Timestamp"].GetItem(1).GetUInt64());

            return new Location()
            {
                Latitude = dbus["Latitude"].GetDouble(),
                Longitude = dbus["Longitude"].GetDouble(),
                Altitude = dbus["Altitude"].GetDouble(),
                Accuracy = dbus["Accuracy"].GetDouble(),
                Speed = dbus["Speed"].GetDouble(),
                Heading = dbus["Heading"].GetDouble(),
                Timestamp = timestamp
            };
        }
    }
}