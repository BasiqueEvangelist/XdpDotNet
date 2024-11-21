using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Tmds.DBus.Protocol;
using XdgDesktopPortal;
using XdpdnExample.ViewModels;
using XdpdnExample.Views;

namespace XdpdnExample;

public partial class App : Application
{
    public static readonly Connection DBusConnection = _getConnection();

    private static Connection _getConnection()
    {
        Connection conn = new Connection(Address.Session!);
        conn.ConnectAsync().AsTask().GetAwaiter().GetResult();
        return conn;
    }

    public static WindowId GetWindowId()
    {
        return WindowId.FromX11(((IClassicDesktopStyleApplicationLifetime)Current!.ApplicationLifetime!)
            .MainWindow!
            .TryGetPlatformHandle()!.ToString());
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow();

        }

        base.OnFrameworkInitializationCompleted();
    }
}