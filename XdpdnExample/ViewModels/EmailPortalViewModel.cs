using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
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

public partial class EmailPortalViewModel : ViewModelBase
{
    public AddressListViewModel Addresses { get; } = new() {Name = "Addresses"};
    public AddressListViewModel Cc { get; } = new() {Name = "Cc"};
    public AddressListViewModel Bcc { get; } = new() {Name = "Bcc"};

    public AddressListViewModel[] AddressLists { get; }

    [ObservableProperty]
    private string? _subject;
    
    [ObservableProperty]
    private string? _body;

    [RelayCommand]
    private async Task ComposeEmail()
    {
        XdpEmail emailSvc = new(App.DBusConnection);

        EmailMessage msg = new();

        msg.Addresses = Addresses.Addresses
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        
        msg.Cc = Cc.Addresses
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        
        msg.Bcc = Bcc.Addresses
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (!string.IsNullOrWhiteSpace(Subject)) msg.Subject = Subject;
        if (!string.IsNullOrWhiteSpace(Body)) msg.Body = Body;

        try
        {
            await emailSvc.ComposeEmail(App.GetWindowId(), msg);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public EmailPortalViewModel()
    {
        AddressLists = [Addresses, Cc, Bcc];
    }
}

public partial class AddressListViewModel : ViewModelBase
{
    public required string Name { get; init; }
    
    public ObservableCollection<EmailAddressViewModel> Addresses { get; } = [];

    [RelayCommand] private void Add() => Addresses.Add(new EmailAddressViewModel(Addresses));
}

public partial class EmailAddressViewModel(ObservableCollection<EmailAddressViewModel> containedIn) : ViewModelBase
{
    [ObservableProperty]
    [EmailAddress]
    private string _value = "";

    [RelayCommand]
    private void Remove()
    {
        containedIn.Remove(this);
    }
}