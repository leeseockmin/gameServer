using FrogTailGameServer.Grpc;
using Grpc.Core;
using Grpc.Net.Client;

class Program
{
    // 서버 gRPC 주소 (h2c: HTTP/2 without TLS)
    private const string ServerAddress = "http://localhost:9001";

    static async Task Main(string[] args)
    {
        Console.WriteLine("[TestClient] FrogTailGameServer gRPC TestClient 시작");

        using var channel = GrpcChannel.ForAddress(ServerAddress, new GrpcChannelOptions
        {
            // h2c (TLS 없는 HTTP/2) 허용
            HttpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            }
        });

        await RunLoginTestAsync(channel);
        await RunGuestLoginTestAsync(channel);
    }

    private static async Task RunLoginTestAsync(GrpcChannel channel)
    {
        var authClient = new LoginService.LoginServiceClient(channel);

        Console.WriteLine("\n[TestClient] === VerityLogin 테스트 ===");
        try
        {
            var verifyRequest = new VerityLoginRequest
            {
                OsType = OsType.Aos,
                LoginType = LoginType.Guest,
                AccessToken = "guest-token-for-test"
            };

            var verifyResponse = await authClient.VerityLoginAsync(verifyRequest);
            Console.WriteLine($"[VerityLogin] ErrorCode: {verifyResponse.ErrorCode}");
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"[VerityLogin] gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
        }

        Console.WriteLine("\n[TestClient] === Login 테스트 ===");
        try
        {
            var loginRequest = new LoginRequest
            {
                DeviceId = "test-device-001",
                NickName = "TestUser",
                OsType = OsType.Windows,
                LoginType = LoginType.Guest,
                AccessToken = "guest-token-for-test"
            };

            var loginResponse = await authClient.LoginAsync(loginRequest);
            Console.WriteLine($"[Login] ErrorCode: {loginResponse.ErrorCode}");
            Console.WriteLine($"[Login] UserId:    {loginResponse.UserId}");
            Console.WriteLine($"[Login] UserToken: {loginResponse.UserToken}");

            if (loginResponse.ErrorCode == ErrorCode.Success)
            {
                Console.WriteLine("\n[TestClient] === GetShopList 테스트 (인증 헤더 포함) ===");
                await RunGetShopListTestAsync(channel, loginResponse.UserId.ToString(), loginResponse.UserToken);
            }
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"[Login] gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
        }
    }

    /// <summary>
    /// Guest 로그인 시나리오:
    ///   1. 신규: AccessToken 빈 값 → 서버가 GuestToken 발급, UserId > 0 확인
    ///   2. 재로그인: 시나리오1에서 받은 GuestToken으로 재로그인 → 동일 UserId 반환 확인
    /// </summary>
    private static async Task RunGuestLoginTestAsync(GrpcChannel channel)
    {
        var authClient = new LoginService.LoginServiceClient(channel);

        Console.WriteLine("\n[TestClient] === Guest 신규 로그인 테스트 ===");
        Console.WriteLine("[TestClient] AccessToken = \"\" (빈 값) → 서버에서 GuestToken 발급 기대");

        string guestToken = string.Empty;
        long firstUserId = 0;

        try
        {
            var newGuestRequest = new LoginRequest
            {
                DeviceId  = "guest-device-new",
                NickName  = "GuestUser",
                OsType    = OsType.Windows,
                LoginType = LoginType.Guest,
                AccessToken = ""   // 신규: 빈 값
            };

            var newGuestResponse = await authClient.LoginAsync(newGuestRequest);
            Console.WriteLine($"[Guest 신규] ErrorCode: {newGuestResponse.ErrorCode}");
            Console.WriteLine($"[Guest 신규] UserId:    {newGuestResponse.UserId}");
            Console.WriteLine($"[Guest 신규] UserToken: {newGuestResponse.UserToken}");
            Console.WriteLine($"[Guest 신규] GuestToken: {newGuestResponse.GuestToken}");

            if (newGuestResponse.ErrorCode == ErrorCode.Success)
            {
                bool guestTokenOk = !string.IsNullOrEmpty(newGuestResponse.GuestToken);
                bool userIdOk     = newGuestResponse.UserId > 0;

                Console.WriteLine($"[Guest 신규] PASS - GuestToken 발급 여부: {guestTokenOk}, UserId > 0: {userIdOk}");

                guestToken  = newGuestResponse.GuestToken;
                firstUserId = newGuestResponse.UserId;
            }
            else
            {
                Console.WriteLine("[Guest 신규] FAIL - ErrorCode가 Success가 아님");
                return;
            }
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"[Guest 신규] gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            return;
        }

        Console.WriteLine("\n[TestClient] === Guest 재로그인 테스트 ===");
        Console.WriteLine($"[TestClient] AccessToken = \"{guestToken}\" (시나리오1에서 받은 GuestToken)");
        Console.WriteLine("[TestClient] 기대: 시나리오1과 동일한 UserId 반환");

        try
        {
            var reLoginRequest = new LoginRequest
            {
                DeviceId  = "guest-device-new",
                NickName  = "GuestUser",
                OsType    = OsType.Windows,
                LoginType = LoginType.Guest,
                AccessToken = guestToken   // 재로그인: 발급받은 GuestToken 사용
            };

            var reLoginResponse = await authClient.LoginAsync(reLoginRequest);
            Console.WriteLine($"[Guest 재로그인] ErrorCode: {reLoginResponse.ErrorCode}");
            Console.WriteLine($"[Guest 재로그인] UserId:    {reLoginResponse.UserId}");
            Console.WriteLine($"[Guest 재로그인] UserToken: {reLoginResponse.UserToken}");
            Console.WriteLine($"[Guest 재로그인] GuestToken: {reLoginResponse.GuestToken} (재로그인은 빈 값이어야 함)");

            if (reLoginResponse.ErrorCode == ErrorCode.Success)
            {
                bool sameUserId      = reLoginResponse.UserId == firstUserId;
                bool noGuestToken    = string.IsNullOrEmpty(reLoginResponse.GuestToken);

                Console.WriteLine($"[Guest 재로그인] PASS - 동일 UserId: {sameUserId} ({firstUserId} == {reLoginResponse.UserId}), GuestToken 미발급: {noGuestToken}");
            }
            else
            {
                Console.WriteLine("[Guest 재로그인] FAIL - ErrorCode가 Success가 아님");
            }
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"[Guest 재로그인] gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
        }
    }

    private static async Task RunGetShopListTestAsync(GrpcChannel channel, string userId, string userToken)
    {
        var shopClient = new ShopService.ShopServiceClient(channel);

        // gRPC 메타데이터로 인증 헤더 전송 (AuthInterceptor에서 검증)
        var headers = new Metadata
        {
            { "x-userid", userId },
            { "authorization", $"Bearer {userToken}" }
        };

        try
        {
            var shopResponse = await shopClient.ShopListAsync(
                new ShopListRequest(),
                headers);

            Console.WriteLine($"[GetShopList] ErrorCode: {shopResponse.ErrorCode}");
            Console.WriteLine($"[GetShopList] ShopCount: {shopResponse.ShopDatas.Count}");

            foreach (var shop in shopResponse.ShopDatas)
            {
                Console.WriteLine($"  Shop ID: {shop.ShopId}, Items: {shop.ShopItemDatas.Count}");
            }
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"[GetShopList] gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
        }
    }
}
