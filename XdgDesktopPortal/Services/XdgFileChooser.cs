using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

public class XdgFileChooser(Connection dbusConnection) {
    private readonly OrgFreedesktopPortalFileChooser Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);
   
    
}