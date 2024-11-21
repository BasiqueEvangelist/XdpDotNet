using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using XdpdnExample.ViewModels;

namespace XdpdnExample.Views;

public partial class EmailPortalView : UserControl
{
    public EmailPortalView()
    {
        DataContext = new EmailPortalViewModel();
        InitializeComponent();
    }
}