using System.Net.Http;
using FrogTailGameServer.Grpc;
using Grpc.Core;
using Grpc.Net.Client;

namespace WpfTestClient.Services;

/// <summary>
/// gRPC 채널 및 서비스 스텁을 단일 지점에서 관리합니다.
/// 서버 주소 변경 시 RebuildChannel()로 채널을 재생성합니다.
/// </summary>
public sealed class GrpcClientService : IDisposable
{
    private GrpcChannel? _channel;
    private string _serverAddress = "http://localhost:9001";

    // 인증이 필요한 요청에 첨부할 세션 정보
    private string? _userId;
    private string? _userToken;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_userId) && !string.IsNullOrEmpty(_userToken);

    // ---------------------------------------------------------------
    // 채널 관리
    // ---------------------------------------------------------------

    /// <summary>
    /// 서버 주소를 변경하고 채널을 재생성합니다.
    /// </summary>
    public void RebuildChannel(string serverAddress)
    {
        _serverAddress = serverAddress;

        _channel?.Dispose();
        _channel = null;

        _channel = BuildChannel(_serverAddress);
    }

    private GrpcChannel GetOrCreateChannel()
    {
        if (_channel is not null)
        {
            return _channel;
        }

        _channel = BuildChannel(_serverAddress);
        return _channel;
    }

    private static GrpcChannel BuildChannel(string address)
    {
        // h2c (TLS 없는 HTTP/2) 허용 설정
        var handler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true
        };

        return GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = handler
        });
    }

    // ---------------------------------------------------------------
    // 세션 정보
    // ---------------------------------------------------------------

    public void SetSession(long userId, string userToken)
    {
        _userId = userId.ToString();
        _userToken = userToken;
    }

    public void ClearSession()
    {
        _userId = null;
        _userToken = null;
    }

    // ---------------------------------------------------------------
    // 인증 헤더 빌더
    // ---------------------------------------------------------------

    /// <summary>
    /// AuthInterceptor가 요구하는 헤더를 구성합니다.
    ///   - x-userid: userId 문자열
    ///   - authorization: Bearer {userToken}
    /// </summary>
    private Metadata BuildAuthHeaders()
    {
        if (!IsAuthenticated)
        {
            throw new InvalidOperationException("세션 정보가 없습니다. 먼저 로그인하세요.");
        }

        return new Metadata
        {
            { "x-userid", _userId! },
            { "authorization", $"Bearer {_userToken}" }
        };
    }

    // ---------------------------------------------------------------
    // LoginService 스텁
    // ---------------------------------------------------------------

    private LoginService.LoginServiceClient GetLoginClient()
        => new(GetOrCreateChannel());

    /// <summary>
    /// Guest 신규 로그인: AccessToken 빈 값 전송 → 서버가 GuestToken 발급
    /// </summary>
    public async Task<LoginResponse> LoginGuestNewAsync(
        string deviceId,
        string nickName,
        CancellationToken ct = default)
    {
        var request = new LoginRequest
        {
            DeviceId    = deviceId,
            NickName    = nickName,
            OsType      = OsType.Windows,
            LoginType   = LoginType.Guest,
            AccessToken = string.Empty   // 신규: 빈 값 → 서버에서 GuestToken 생성
        };

        return await GetLoginClient().LoginAsync(request, cancellationToken: ct);
    }

    /// <summary>
    /// Guest 재로그인: 이전에 발급받은 GuestToken을 AccessToken으로 전송
    /// </summary>
    public async Task<LoginResponse> LoginGuestReLoginAsync(
        string deviceId,
        string nickName,
        string guestToken,
        CancellationToken ct = default)
    {
        var request = new LoginRequest
        {
            DeviceId    = deviceId,
            NickName    = nickName,
            OsType      = OsType.Windows,
            LoginType   = LoginType.Guest,
            AccessToken = guestToken   // 재로그인: 발급받은 GuestToken 사용
        };

        return await GetLoginClient().LoginAsync(request, cancellationToken: ct);
    }

    /// <summary>
    /// Firebase AccessToken 검증 (Anonymous 메서드 — 인증 헤더 불필요)
    /// </summary>
    public async Task<VerityLoginResponse> VerityLoginAsync(
        LoginType loginType,
        string accessToken,
        CancellationToken ct = default)
    {
        var request = new VerityLoginRequest
        {
            OsType      = OsType.Windows,
            LoginType   = loginType,
            AccessToken = accessToken
        };

        return await GetLoginClient().VerityLoginAsync(request, cancellationToken: ct);
    }

    // ---------------------------------------------------------------
    // ShopService 스텁
    // ---------------------------------------------------------------

    private ShopService.ShopServiceClient GetShopClient()
        => new(GetOrCreateChannel());

    /// <summary>
    /// 인증 헤더를 포함하여 ShopList를 조회합니다.
    /// 로그인 후 SetSession()이 호출된 상태여야 합니다.
    /// </summary>
    public async Task<ShopListResponse> GetShopListAsync(CancellationToken ct = default)
    {
        var headers = BuildAuthHeaders();
        return await GetShopClient().ShopListAsync(
            new ShopListRequest(),
            headers,
            cancellationToken: ct);
    }

    // ---------------------------------------------------------------
    // IDisposable
    // ---------------------------------------------------------------

    public void Dispose()
    {
        _channel?.Dispose();
        _channel = null;
    }
}
