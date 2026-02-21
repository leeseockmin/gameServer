using FrogTailGameServer.Grpc;
using Grpc.Core;
using Grpc.Net.Client;

class Program
{
    private const string ServerAddress = "http://localhost:9001";

    static async Task Main(string[] args)
    {
        Console.WriteLine("[TestClient] FrogTailGameServer gRPC TestClient 시작");

        using var channel = GrpcChannel.ForAddress(ServerAddress, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            }
        });

        await RunLoginScenariosAsync(channel);
    }

    /// <summary>
    /// 통합 로그인 시나리오:
    ///   1. Guest 신규 로그인 (AccessToken = "") → GuestToken 발급 확인
    ///   2. ShopList 조회 (신규 로그인 세션으로 인증 확인)
    ///   3. Guest 재로그인 (AccessToken = GuestToken) → 동일 UserId 확인
    ///   4. ShopList 조회 (재로그인 세션으로 인증 확인)
    /// </summary>
    private static async Task RunLoginScenariosAsync(GrpcChannel channel)
    {
        var authClient = new LoginService.LoginServiceClient(channel);

        // ------------------------------------------------------------------
        // 시나리오 1: Guest 신규 로그인
        // ------------------------------------------------------------------
        Console.WriteLine("\n[TestClient] === [1/4] Guest 신규 로그인 ===");
        Console.WriteLine("[TestClient] AccessToken = \"\" (빈 값) → 서버에서 GuestToken 발급 기대");

        string guestToken  = string.Empty;
        long   firstUserId = 0;
        string userToken1  = string.Empty;

        try
        {
            var response = await authClient.LoginAsync(new LoginRequest
            {
                DeviceId    = "guest-device-001",
                NickName    = "GuestUser",
                OsType      = OsType.Windows,
                LoginType   = LoginType.Guest,
                AccessToken = string.Empty
            });

            Console.WriteLine($"[1/4] ErrorCode:  {response.ErrorCode}");
            Console.WriteLine($"[1/4] UserId:     {response.UserId}");
            Console.WriteLine($"[1/4] UserToken:  {response.UserToken}");
            Console.WriteLine($"[1/4] GuestToken: {response.GuestToken}");

            if (response.ErrorCode != ErrorCode.Success)
            {
                Console.WriteLine("[1/4] FAIL - 이후 시나리오 중단");
                return;
            }

            Console.WriteLine($"[1/4] PASS - GuestToken 발급: {!string.IsNullOrEmpty(response.GuestToken)}, UserId > 0: {response.UserId > 0}");
            guestToken  = response.GuestToken;
            firstUserId = response.UserId;
            userToken1  = response.UserToken;
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"[1/4] gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            return;
        }

        // ------------------------------------------------------------------
        // 시나리오 2: ShopList 조회 (신규 로그인 세션)
        // ------------------------------------------------------------------
        Console.WriteLine("\n[TestClient] === [2/4] ShopList 조회 (신규 로그인 세션) ===");
        await RunGetShopListTestAsync(channel, firstUserId.ToString(), userToken1);

        // ------------------------------------------------------------------
        // 시나리오 3: Guest 재로그인
        // ------------------------------------------------------------------
        Console.WriteLine("\n[TestClient] === [3/4] Guest 재로그인 ===");
        Console.WriteLine($"[TestClient] AccessToken = \"{guestToken}\" → 동일 UserId 기대");

        string userToken2 = string.Empty;

        try
        {
            var response = await authClient.LoginAsync(new LoginRequest
            {
                DeviceId    = "guest-device-001",
                NickName    = "GuestUser",
                OsType      = OsType.Windows,
                LoginType   = LoginType.Guest,
                AccessToken = guestToken
            });

            Console.WriteLine($"[3/4] ErrorCode:  {response.ErrorCode}");
            Console.WriteLine($"[3/4] UserId:     {response.UserId}");
            Console.WriteLine($"[3/4] GuestToken: {response.GuestToken} (재로그인은 빈 값이어야 함)");

            if (response.ErrorCode != ErrorCode.Success)
            {
                Console.WriteLine("[3/4] FAIL");
                return;
            }

            bool sameUserId   = response.UserId == firstUserId;
            bool noGuestToken = string.IsNullOrEmpty(response.GuestToken);
            Console.WriteLine($"[3/4] PASS - 동일 UserId: {sameUserId} ({firstUserId}=={response.UserId}), GuestToken 미발급: {noGuestToken}");
            userToken2 = response.UserToken;
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"[3/4] gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            return;
        }

        // ------------------------------------------------------------------
        // 시나리오 4: ShopList 조회 (재로그인 세션)
        // ------------------------------------------------------------------
        Console.WriteLine("\n[TestClient] === [4/4] ShopList 조회 (재로그인 세션) ===");
        await RunGetShopListTestAsync(channel, firstUserId.ToString(), userToken2);
    }

    private static async Task RunGetShopListTestAsync(GrpcChannel channel, string userId, string userToken)
    {
        var shopClient = new ShopService.ShopServiceClient(channel);
        var headers = new Metadata
        {
            { "x-userid",      userId },
            { "authorization", $"Bearer {userToken}" }
        };

        try
        {
            var response = await shopClient.ShopListAsync(new ShopListRequest(), headers);
            Console.WriteLine($"  ErrorCode: {response.ErrorCode}");
            Console.WriteLine($"  ShopCount: {response.ShopDatas.Count}");
            foreach (var shop in response.ShopDatas)
                Console.WriteLine($"  Shop ID: {shop.ShopId}, Items: {shop.ShopItemDatas.Count}");
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"  gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
        }
    }
}
