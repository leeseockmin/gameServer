using System.Windows;
using System.Windows.Controls;
using FrogTailGameServer.Grpc;
using WpfTestClient.ViewModels;

namespace WpfTestClient;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // LoginType enum을 ComboBox ItemsSource로 설정
        // XAML ObjectDataProvider 방식은 어셈블리 참조 문제가 있어 code-behind에서 설정
        LoginTypeCombo.ItemsSource = Enum.GetValues<LoginType>();
    }

    private void RequestTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.ScrollToEnd();
    }

    private void ResponseTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.ScrollToEnd();
    }
}
