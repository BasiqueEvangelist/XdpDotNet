using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XdgDesktopPortal.Services;

namespace XdpdnExample.ViewModels;

public partial class SecretPortalViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _secretHex = "";

    [RelayCommand]
    private async Task RetrieveSecret()
    {
        XdpSecret secretPortal = new(App.DBusConnection);

        var secret = await secretPortal.RetrieveSecret();
        SecretHex = Convert.ToHexString(secret);
    }
}
