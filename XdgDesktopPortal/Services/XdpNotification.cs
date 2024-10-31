using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public class XdpNotification(Connection dbusConnection)
{
    private readonly OrgFreedesktopPortalNotification Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);

    public Task AddNotification(string id, Notification notification)
    {
        return Wrapped.AddNotificationAsync(id, notification.ToDBus());
    }

    public Task RemoveNotification(string id)
    {
        return Wrapped.RemoveNotificationAsync(id);
    }

    // TODO: ActionInvoked signal

    public class Notification
    {
        public string? Title { get; set; }
        public string? Body { get; set; }
        public byte[]? Icon { get; set; } // TODO: GThemedIcon?
        public NotificationPriority? Priority { get; set; }
        public string? DefaultAction { get; set; }
        public Variant? DefaultActionTarget { get; set; }
        public List<NotificationButton> Buttons { get; set; } = [];

        internal Dictionary<string, Variant> ToDBus()
        {
            Dictionary<string, Variant> dict = new();

            if (Title != null) dict["title"] = Title;
            if (Body != null) dict["body"] = Body;
            if (Icon != null) dict["icon"] = new Struct<string, Variant>("bytes", new Tmds.DBus.Protocol.Array<byte>(Icon)).AsVariant();
            if (Priority != null) dict["priority"] = Priority.Value.ToString().ToLowerInvariant();
            if (DefaultAction != null) dict["default-action"] = DefaultAction;
            if (DefaultActionTarget != null) dict["default-action-target"] = DefaultActionTarget.Value;
            if (Buttons.Count > 0) dict["buttons"] = new Array<Dictionary<string, Variant>>(Buttons.Select(x => x.ToDBus()));

            return dict;
        }
    }

    public class NotificationButton
    {
        public required string Label { get; set; }
        public required string Action { get; set; }
        public Variant? Target { get; set; }

        internal Dictionary<string, Variant> ToDBus()
        {
            Dictionary<string, Variant> dict = new();

            dict["label"] = Label;
            dict["action"] = Action;

            if (Target != null) dict["target"] = Target.Value;

            return dict;
        }
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
}