using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XdgDesktopPortal;
using XdgDesktopPortal.Services;

namespace XdpdnExample.ViewModels;

public partial class AccountPortalViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _reason;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InfoAvailable))]
    [NotifyPropertyChangedFor(nameof(AccountImage))]
    private XdgAccountInformation? _info;

    public bool InfoAvailable => Info != null;

    public Bitmap? AccountImage
    {
        get
        {
            if (Info == null) return null;
            if (Info.Image.Scheme != "file") throw new FileNotFoundException("ae");

            return new Bitmap(File.OpenRead(Info.Image.AbsolutePath));
        }
    }

    [RelayCommand]
    private async Task AskForUserInformation()
    {
        XdpAccount accountSvc = new(App.DBusConnection);
        
        Info = await accountSvc.GetUserInformation(App.GetWindowId(), Reason);
    }
}
