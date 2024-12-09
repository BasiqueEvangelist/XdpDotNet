using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XdgDesktopPortal.Services;

namespace XdpdnExample.ViewModels;

public partial class TrashPortalViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedFileName))]
    [NotifyCanExecuteChangedFor(nameof(TrashCommand))]
    private IStorageFile? _selectedFile;

    public string SelectedFileName => SelectedFile == null ? "" : SelectedFile.Name;
    
    [RelayCommand]
    private async Task PickFile()
    {
        var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
        var res = await window.StorageProvider.OpenFilePickerAsync(new());
        SelectedFile = res.Count > 0 ? res[0] : null;
    }

    public bool CanTrash => SelectedFile != null;
    
    [RelayCommand(CanExecute = nameof(CanTrash))]
    private async Task Trash()
    {
        if (SelectedFile is null) return;
        
        string? path = SelectedFile.TryGetLocalPath();
        if (path == null) return;

        XdpTrash trash = new(App.DBusConnection);
        
        await using var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete);
        await trash.TrashFile(stream.SafeFileHandle);
    }
}
