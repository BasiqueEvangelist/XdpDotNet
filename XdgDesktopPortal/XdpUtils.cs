namespace XdgDesktopPortal;

internal class XdpUtils
{
    public const string Destination = "org.freedesktop.portal.Desktop";
    public const string DesktopObject = "/org/freedesktop/portal/desktop";
    
    public const string DocumentsDestination = "org.freedesktop.portal.Documents";
    public const string DocumentsObject = "/org/freedesktop/portal/documents";
    
    public static string GenerateHandleToken()
    {
        Span<char> token = stackalloc char[16];
        Random.Shared.GetItems("abcdefghijklmnopqrstuvwxyz0123456789", token);
        return $"xdp_dot_net_{token}";
    }
}
