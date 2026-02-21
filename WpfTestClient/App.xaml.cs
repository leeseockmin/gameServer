using System.Windows;
using WpfTestClient.Services;
using WpfTestClient.ViewModels;

namespace WpfTestClient;

/// <summary>
/// Application 진입점.
/// DI 컨테이너 없이 수동 Composition Root 패턴으로 의존성을 주입합니다.
/// (테스트 클라이언트 목적상 Microsoft.Extensions.DependencyInjection 미사용)
/// </summary>
public partial class App : Application
{
    private GrpcClientService? _grpcService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _grpcService = new GrpcClientService();
        var viewModel = new MainViewModel(_grpcService);

        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _grpcService?.Dispose();
        base.OnExit(e);
    }
}
