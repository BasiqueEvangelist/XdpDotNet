using Avalonia.Controls;
using XdpdnExample.ViewModels;

namespace XdpdnExample.Views;

public partial class TrashPortalView : UserControl
{
    public TrashPortalView()
    {
        DataContext = new TrashPortalViewModel();
        InitializeComponent();
    }
}