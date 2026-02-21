using System.Windows;
using WpfTestClient.ViewModels;

namespace WpfTestClient.Views;

public partial class PacketListWindow : Window
{
    public PacketListWindow()
    {
        InitializeComponent();
        DataContext = new PacketListViewModel();
    }

    public PacketListViewModel ViewModel => (PacketListViewModel)DataContext;
}
