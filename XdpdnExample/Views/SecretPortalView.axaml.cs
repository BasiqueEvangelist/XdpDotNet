using Avalonia.Controls;
using XdpdnExample.ViewModels;

namespace XdpdnExample.Views;

public partial class SecretPortalView : UserControl
{
    public SecretPortalView()
    {
        DataContext = new SecretPortalViewModel();
        InitializeComponent();
    }
}