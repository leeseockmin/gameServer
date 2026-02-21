using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrogTailGameServer.Grpc;
using Grpc.Core;
using WpfTestClient.Models;
using WpfTestClient.Services;
using WpfTestClient.Views;

namespace WpfTestClient.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly GrpcClientService _grpcService;

    [ObservableProperty]
    private string _serverAddress = "http://localhost:9001";

    [ObservableProperty]
    private string _deviceId = "wpf-test-device-001";

    [ObservableProperty]
    private string _nickName = "WpfTestUser";

    [ObservableProperty]
    private LoginType _loginType = LoginType.Guest;

    /// <summary>
    /// Guest 신규: 비워두면 서버에서 GuestToken 발급 후 여기에 자동 채움.
    /// Guest 재로그인: 자동 채워진 GuestToken 값.
    /// Firebase: JWT AccessToken.
    /// </summary>
    [ObservableProperty]
    private string _accessToken = string.Empty;

    [ObservableProperty]
    private string _requestLog = string.Empty;

    [ObservableProperty]
    private string _responseLog = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(LoginCommand),
        nameof(GetShopListCommand),
        nameof(RunScenarioCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _sessionInfo = "세션 없음";

    // 시나리오 큐
    public ObservableCollection<PacketItem> ScenarioQueue { get; } = [];

    public string ScenarioSummary
        => ScenarioQueue.Count == 0
            ? "(시나리오 없음)"
            : string.Join(" -> ", ScenarioQueue.Select(p => p.RpcName));

    public MainViewModel(GrpcClientService grpcService)
    {
        _grpcService = grpcService;
        ScenarioQueue.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ScenarioSummary));
    }

    // ---------------------------------------------------------------
    // 채널 재구성
    // ---------------------------------------------------------------

    [RelayCommand]
    private void RebuildChannel()
    {
        if (string.IsNullOrWhiteSpace(ServerAddress))
        {
            AppendResponse("[RebuildChannel] 서버 주소를 입력하세요.");
            return;
        }
        _grpcService.RebuildChannel(ServerAddress);
        AppendResponse($"[RebuildChannel] 채널 재구성 완료: {ServerAddress}");
    }

    // ---------------------------------------------------------------
    // 단일 로그인
    // ---------------------------------------------------------------

    private bool CanExecuteRequest() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanExecuteRequest))]
    private async Task LoginAsync()
    {
        IsBusy = true;
        _grpcService.ClearSession();
        SessionInfo = "세션 없음";

        AppendRequest("[Login] 요청");
        AppendRequest($"  LoginType:   {LoginType}");
        AppendRequest($"  DeviceId:    {DeviceId}");
        AppendRequest($"  NickName:    {NickName}");
        AppendRequest($"  AccessToken: {(string.IsNullOrEmpty(AccessToken) ? "\"\" (신규)" : Truncate(AccessToken, 16) + "...")}");
        

		try
        {
            var response = await _grpcService.LoginAsync(LoginType, DeviceId, NickName, AccessToken);

            AppendResponse("[Login] 응답");
            AppendResponse($"  ErrorCode:  {response.ErrorCode}");
            AppendResponse($"  UserId:     {response.UserId}");
            AppendResponse($"  UserToken:  {response.UserToken}");
            AppendResponse($"  GuestToken: {response.GuestToken}");

            if (response.ErrorCode == ErrorCode.Success)
            {
                _grpcService.SetSession(response.UserId, response.UserToken);
                SessionInfo = $"LoginType={LoginType} | UserId={response.UserId} | Token={Truncate(response.UserToken, 12)}...";

                if (!string.IsNullOrEmpty(response.GuestToken))
                {
                    AccessToken = response.GuestToken;
                    AppendResponse("  [안내] GuestToken이 AccessToken 필드에 자동으로 채워졌습니다. 재로그인 시 그대로 사용하세요.");
                }
            }
            else
            {
                AppendResponse("  [FAIL] ErrorCode가 Success가 아님");
            }
        }
        catch (RpcException ex)
        {
            AppendResponse($"  [gRPC Error] {ex.Status.StatusCode} — {ex.Status.Detail}");
        }
        catch (Exception ex)
        {
            AppendResponse($"  [Error] {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------------
    // ShopList 조회
    // ---------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(CanExecuteRequest))]
    private async Task GetShopListAsync()
    {
        if (!_grpcService.IsAuthenticated)
        {
            AppendResponse("[GetShopList] 먼저 로그인하세요. 세션 없음.");
            return;
        }

        IsBusy = true;
        AppendRequest("[GetShopList] 요청");
        AppendRequest("  헤더: x-userid, authorization: Bearer ***");

        try
        {
            var response = await _grpcService.GetShopListAsync();
            AppendResponse("[GetShopList] 응답");
            AppendResponse($"  ErrorCode: {response.ErrorCode}");
            AppendResponse($"  ShopCount: {response.ShopDatas.Count}");
            foreach (var shop in response.ShopDatas)
            {
                AppendResponse($"    ShopId={shop.ShopId}, ItemCount={shop.ShopItemDatas.Count}");
                foreach (var item in shop.ShopItemDatas)
                    AppendResponse($"      ItemId={item.ShopItemId}, BuyCount={item.BuyCount}");
            }
        }
        catch (RpcException ex)
        {
            AppendResponse($"  [gRPC Error] {ex.Status.StatusCode} — {ex.Status.Detail}");
        }
        catch (Exception ex)
        {
            AppendResponse($"  [Error] {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------------
    // 시나리오 구성 팝업
    // ---------------------------------------------------------------

    [RelayCommand]
    private void OpenPacketList()
    {
        var window = new PacketListWindow();

        foreach (var item in ScenarioQueue)
            window.ViewModel.ScenarioQueue.Add(item);

        bool? result = window.ShowDialog();

        if (result == true)
        {
            ScenarioQueue.Clear();
            foreach (var item in window.ViewModel.ScenarioQueue)
                ScenarioQueue.Add(item);
        }
    }

    // ---------------------------------------------------------------
    // 시나리오 실행
    // ---------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(CanExecuteRequest))]
    private async Task RunScenarioAsync()
    {
        if (ScenarioQueue.Count == 0)
        {
            AppendResponse("[시나리오] 실행할 패킷이 없습니다. 시나리오를 먼저 구성하세요.");
            return;
        }

        IsBusy = true;
        int total = ScenarioQueue.Count;
        AppendResponse($"[시나리오] 시작 — 총 {total}개 패킷");

        for (int i = 0; i < total; i++)
        {
            var packet = ScenarioQueue[i];
            AppendResponse($"[{i + 1}/{total}] {packet.DisplayName} 실행 중...");
            try
            {
                await ExecutePacketAsync(packet);
            }
            catch (RpcException ex)
            {
                AppendResponse($"  [gRPC Error] {ex.Status.StatusCode} — {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                AppendResponse($"  [Error] {ex.Message}");
            }
        }

        AppendResponse("[시나리오] 완료");
        IsBusy = false;
    }

    private async Task ExecutePacketAsync(PacketItem packet)
    {
        if (packet.RequiresAuth && !_grpcService.IsAuthenticated)
        {
            AppendResponse($"  [경고] 세션 없음 — {packet.DisplayName} 스킵.");
            return;
        }

        switch (packet.DisplayName)
        {
            case "LoginService.Login":
            {
                var response = await _grpcService.LoginAsync(LoginType, DeviceId, NickName, AccessToken);
                AppendResponse($"  ErrorCode: {response.ErrorCode}, UserId: {response.UserId}");
                if (response.ErrorCode == ErrorCode.Success)
                {
                    _grpcService.SetSession(response.UserId, response.UserToken);
                    SessionInfo = $"LoginType={LoginType} | UserId={response.UserId} | Token={Truncate(response.UserToken, 12)}...";
                    if (!string.IsNullOrEmpty(response.GuestToken))
                        AccessToken = response.GuestToken;
                }
                break;
            }
            case "LoginService.VerityLogin":
            {
                var response = await _grpcService.VerityLoginAsync(LoginType, AccessToken);
                AppendResponse($"  ErrorCode: {response.ErrorCode}");
                break;
            }
            case "ShopService.ShopList":
            {
                var response = await _grpcService.GetShopListAsync();
                AppendResponse($"  ErrorCode: {response.ErrorCode}, ShopCount: {response.ShopDatas.Count}");
                break;
            }
            default:
                AppendResponse($"  [경고] 알 수 없는 패킷: {packet.DisplayName}. 스킵.");
                break;
        }
    }

    // ---------------------------------------------------------------
    // 로그 초기화
    // ---------------------------------------------------------------

    [RelayCommand]
    private void ClearLog()
    {
        RequestLog  = string.Empty;
        ResponseLog = string.Empty;
    }

    // ---------------------------------------------------------------
    // 헬퍼
    // ---------------------------------------------------------------

    private void AppendRequest(string message)
        => RequestLog += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";

    private void AppendResponse(string message)
        => ResponseLog += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
