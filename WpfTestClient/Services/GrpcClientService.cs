using System.Net.Http;
using FrogTailGameServer.Grpc;
using Grpc.Core;
using Grpc.Net.Client;

namespace WpfTestClient.Services;

public sealed class GrpcClientService : IDisposable
{
    private GrpcChannel? _channel;
    private string _serverAddress = "http://localhost:9001";

    private string? _userId;
    private string? _userToken;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_userId) && !string.IsNullOrEmpty(_userToken);

    public void RebuildChannel(string serverAddress)
    {
        _serverAddress = serverAddress;
        _channel?.Dispose();
        _channel = BuildChannel(_serverAddress);
    }

    private GrpcChannel GetOrCreateChannel()
    {
        if (_channel is not null)
            return _channel;
        _channel = BuildChannel(_serverAddress);
        return _channel;
    }

    private static GrpcChannel BuildChannel(string address)
    {
        var handler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true
        };
        return GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = handler
        });
    }

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

    private Metadata BuildAuthHeaders()
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("세션 정보가 없습니다. 먼저 로그인하세요.");

        return new Metadata
        {
            { "x-userid",      _userId! },
            { "authorization", $"Bearer {_userToken}" }
        };
    }

    private LoginService.LoginServiceClient GetLoginClient()
        => new(GetOrCreateChannel());

    /// <summary>
    /// 단일 로그인 메서드.
    /// Guest 신규: loginType=Guest, accessToken=""
    /// Guest 재로그인: loginType=Guest, accessToken={guestToken}
    /// Firebase 계정: loginType=Google/..., accessToken={firebaseToken}
    /// </summary>
    public async Task<LoginResponse> LoginAsync(
        LoginType loginType,
        string deviceId,
        string nickName,
        string accessToken,
        CancellationToken ct = default)
    {
        var request = new LoginRequest
        {
            LoginType   = loginType,
            OsType      = OsType.Windows,
            DeviceId    = deviceId,
            NickName    = nickName,
            AccessToken = accessToken
        };
        return await GetLoginClient().LoginAsync(request, cancellationToken: ct);
    }

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

    private ShopService.ShopServiceClient GetShopClient()
        => new(GetOrCreateChannel());

    public async Task<ShopListResponse> GetShopListAsync(CancellationToken ct = default)
    {
        var headers = BuildAuthHeaders();
        return await GetShopClient().ShopListAsync(
            new ShopListRequest(), headers, cancellationToken: ct);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _channel = null;
    }
}
