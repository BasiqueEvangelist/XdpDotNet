namespace XdgDesktopPortal;

public struct WindowId
{
    public static WindowId FromX11(string? xid) => new WindowId($"x11:{xid}");
    public static WindowId FromWayland(string handle) => new WindowId($"wayland:{handle}");

    public string Value => value ?? "";

    private readonly string value;

    private WindowId(string value)
    {
        this.value = value;
    }
}