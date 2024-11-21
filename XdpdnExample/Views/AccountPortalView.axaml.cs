using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using XdpdnExample.ViewModels;

namespace XdpdnExample.Views;

public partial class AccountPortalView : UserControl
{
    public AccountPortalView()
    {
        DataContext = new AccountPortalViewModel();
        InitializeComponent();
    }
}