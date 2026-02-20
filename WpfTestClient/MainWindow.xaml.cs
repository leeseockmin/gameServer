using System.Windows;
using System.Windows.Controls;
using WpfTestClient.ViewModels;

namespace WpfTestClient;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    // 요청 로그가 갱신될 때 자동 스크롤
    private void RequestTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.ScrollToEnd();
        }
    }

    // 응답 로그가 갱신될 때 자동 스크롤
    private void ResponseTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.ScrollToEnd();
        }
    }
}
