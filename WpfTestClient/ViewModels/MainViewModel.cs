using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrogTailGameServer.Grpc;
using Grpc.Core;
using WpfTestClient.Services;

namespace WpfTestClient.ViewModels;

/// <summary>
/// MainWindow의 DataContext.
/// CommunityToolkit.Mvvm의 소스 생성기를 활용합니다.
///   - [ObservableProperty] → 자동으로 프로퍼티 + INotifyPropertyChanged 생성
///   - [RelayCommand]       → 자동으로 ICommand 구현체 생성
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly GrpcClientService _grpcService;

    // ---------------------------------------------------------------
    // Observable 상태
    // ---------------------------------------------------------------

    [ObservableProperty]
    private string _serverAddress = "http://localhost:9001";

    [ObservableProperty]
    private string _deviceId = "wpf-test-device-001";

    [ObservableProperty]
    private string _nickName = "WpfTestUser";

    /// <summary>
    /// Guest 재로그인 시 입력하는 GuestToken 필드.
    /// 신규 로그인 성공 후 자동으로 채워집니다.
    /// </summary>
    [ObservableProperty]
    private string _guestToken = string.Empty;

    [ObservableProperty]
    private string _requestLog = string.Empty;

    [ObservableProperty]
    private string _responseLog = string.Empty;

    /// <summary>
    /// 요청 진행 중 버튼 비활성화용 플래그.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(LoginGuestNewCommand),
        nameof(LoginGuestReLoginCommand),
        nameof(GetShopListCommand))]
    private bool _isBusy;

    // ---------------------------------------------------------------
    // 내부 세션 상태 (화면 표시용)
    // ---------------------------------------------------------------

    [ObservableProperty]
    private string _sessionInfo = "세션 없음";

    // ---------------------------------------------------------------
    // 생성자
    // ---------------------------------------------------------------

    public MainViewModel(GrpcClientService grpcService)
    {
        _grpcService = grpcService;
    }

    // ---------------------------------------------------------------
    // 채널 재구성 Command
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
    // Guest 신규 로그인 Command
    // ---------------------------------------------------------------

    private bool CanExecuteRequest() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanExecuteRequest))]
    private async Task LoginGuestNewAsync()
    {
        IsBusy = true;
        _grpcService.ClearSession();
        SessionInfo = "세션 없음";

        AppendRequest("[Guest 신규 로그인] 요청");
        AppendRequest($"  DeviceId: {DeviceId}");
        AppendRequest($"  NickName: {NickName}");
        AppendRequest($"  AccessToken: \"\" (빈 값 — 서버에서 GuestToken 발급 기대)");

        try
        {
            var response = await _grpcService.LoginGuestNewAsync(DeviceId, NickName);
            AppendResponse("[Guest 신규 로그인] 응답");
            AppendResponse($"  ErrorCode:  {response.ErrorCode}");
            AppendResponse($"  UserId:     {response.UserId}");
            AppendResponse($"  UserToken:  {response.UserToken}");
            AppendResponse($"  GuestToken: {response.GuestToken}");

            if (response.ErrorCode == ErrorCode.Success)
            {
                _grpcService.SetSession(response.UserId, response.UserToken);
                GuestToken = response.GuestToken;   // 재로그인 입력란에 자동 채움
                SessionInfo = $"UserId={response.UserId} | Token={Truncate(response.UserToken, 12)}...";

                bool guestTokenOk = !string.IsNullOrEmpty(response.GuestToken);
                bool userIdOk = response.UserId > 0;
                AppendResponse($"  [검증] GuestToken 발급: {guestTokenOk}, UserId > 0: {userIdOk}");
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
    // Guest 재로그인 Command
    // ---------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(CanExecuteRequest))]
    private async Task LoginGuestReLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(GuestToken))
        {
            AppendResponse("[Guest 재로그인] GuestToken 입력 필요");
            return;
        }

        IsBusy = true;
        _grpcService.ClearSession();
        SessionInfo = "세션 없음";

        AppendRequest("[Guest 재로그인] 요청");
        AppendRequest($"  DeviceId:    {DeviceId}");
        AppendRequest($"  NickName:    {NickName}");
        AppendRequest($"  AccessToken: {GuestToken}");

        try
        {
            var response = await _grpcService.LoginGuestReLoginAsync(DeviceId, NickName, GuestToken);
            AppendResponse("[Guest 재로그인] 응답");
            AppendResponse($"  ErrorCode:  {response.ErrorCode}");
            AppendResponse($"  UserId:     {response.UserId}");
            AppendResponse($"  UserToken:  {response.UserToken}");
            AppendResponse($"  GuestToken: {response.GuestToken} (재로그인은 빈 값이어야 함)");

            if (response.ErrorCode == ErrorCode.Success)
            {
                _grpcService.SetSession(response.UserId, response.UserToken);
                SessionInfo = $"UserId={response.UserId} | Token={Truncate(response.UserToken, 12)}...";

                bool noGuestToken = string.IsNullOrEmpty(response.GuestToken);
                AppendResponse($"  [검증] GuestToken 미발급: {noGuestToken}");
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
    // ShopList 조회 Command
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
                {
                    AppendResponse($"      ItemId={item.ShopItemId}, BuyCount={item.BuyCount}");
                }
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
    // 로그 초기화 Command
    // ---------------------------------------------------------------

    [RelayCommand]
    private void ClearLog()
    {
        RequestLog = string.Empty;
        ResponseLog = string.Empty;
    }

    // ---------------------------------------------------------------
    // 내부 헬퍼
    // ---------------------------------------------------------------

    private void AppendRequest(string message)
    {
        RequestLog += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
    }

    private void AppendResponse(string message)
    {
        ResponseLog += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
