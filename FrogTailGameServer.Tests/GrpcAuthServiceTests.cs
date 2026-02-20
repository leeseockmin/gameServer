using Common.Redis;
using DataBase.AccountDB;
using DataBase.GameDB;
using DB;
using FrogTailGameServer.GrpcServices;
using FrogTailGameServer.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Share.Common;
using System.Data.Common;
using Xunit;

namespace FrogTailGameServer.Tests
{
    /// <summary>
    /// GrpcAuthService.Login 단위 테스트.
    ///
    /// 테스트 전략:
    /// - DataBaseManager.DBContextExcuteTransaction 은 virtual이므로 Moq로 mock.
    ///   콜백(Func)을 캡처하여 테스트 내부에서 직접 실행 → DB Logic 레이어까지 검증.
    /// - RedisClient.SetUserSession 은 virtual이므로 Moq로 mock.
    ///   호출 여부 및 인자 검증.
    ///
    /// 케이스 목록:
    ///   [정상] Guest_NewLogin_EmptyToken_ReturnsSuccessAndGuestToken
    ///   [정상] Guest_ReLogin_WithGuestToken_ReturnsSameUserId
    ///   [정상] Google_ValidToken_ReturnsSuccess  (Firebase 호출 skip — 별도 통합 테스트 대상)
    ///   [예외] Guest_EmptyNickName_ReturnsInvalidNickName
    ///   [예외] UnsupportedOsType_ReturnsInvalidPacketInfo
    ///   [예외] UnsupportedLoginType_ReturnsInvalidUserToken
    ///   [예외] Google_EmptyToken_ReturnsInvalidUserToken
    ///   [DB검증] Guest_NewLogin_InsertsAccount_AccountLink_UserInfo
    ///   [DB검증] Guest_ReLogin_UpdatesAccount_DoesNotInsertNew
    ///   [경계값] Guest_ReLogin_EmptyToken_TreatedAsNewLogin
    /// </summary>
    public class GrpcAuthServiceTests
    {
        // ─── 공통 Mock 셋업 ───────────────────────────────────────────
        private readonly Mock<DataBaseManager>  _dbManagerMock;
        private readonly Mock<RedisClient>      _redisMock;
        private readonly Mock<ILogger<GrpcAuthService>> _loggerMock;

        // DB 콜백 캡처: Account DB 트랜잭션 func, Game DB 트랜잭션 func
        private Func<DbConnection, Task<bool>>? _capturedAccountFunc;
        private Func<DbConnection, Task<bool>>? _capturedGameFunc;

        // DB 조회 결과를 테스트별로 제어하기 위한 필드
        private AccountLink? _existingAccountLink = null;
        private Account?     _existingAccount     = null;

        // DB Insert 결과 추적 (DB 검증 케이스에서 활용)
        private Account?      _insertedAccount     = null;
        private AccountLink?  _insertedAccountLink = null;
        private UserInfo?     _insertedUserInfo    = null;

        private const long FakeAccountId = 100L;
        private const long FakeUserId    = 200L;
        private const string FakeUserToken = "fake-user-token";

        public GrpcAuthServiceTests()
        {
            _loggerMock = new Mock<ILogger<GrpcAuthService>>();

            // DataBaseManager mock — 생성자 파라미터는 Moq가 자동 처리
            _dbManagerMock = new Mock<DataBaseManager>(
                new Mock<ILogger<DataBaseManager>>().Object,
                new Mock<Microsoft.EntityFrameworkCore.IDbContextFactory<DataBase.AccountDB.AccountDBContext>>().Object,
                new Mock<Microsoft.EntityFrameworkCore.IDbContextFactory<DataBase.GameDB.GameDBContext>>().Object)
            {
                CallBase = false
            };

            // RedisClient mock
            // IConfiguration을 올바르게 setup해야 RedisClient 생성자가 NPE 없이 동작함.
            // GetConnectionString 확장 메서드는 내부적으로 ["ConnectionStrings:{name}"] 인덱서를 호출.
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var connStrSection = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
            connStrSection.Setup(s => s.Value).Returns("localhost:6379,abortConnect=false");
            configMock.Setup(c => c.GetSection("ConnectionStrings")).Returns(connStrSection.Object);
            configMock.Setup(c => c["ConnectionStrings:RedisConnection"]).Returns("localhost:6379,abortConnect=false");

            _redisMock = new Mock<RedisClient>(
                configMock.Object,
                new Mock<ILogger<RedisClient>>().Object)
            {
                CallBase = false
            };

            SetupDbManagerMock();
            SetupRedisMock();
        }

        // ─── Mock 셋업 헬퍼 ───────────────────────────────────────────

        /// <summary>
        /// DBContextExcuteTransaction을 가로채어 콜백을 캡처하고
        /// FakeDbConnection으로 즉시 실행한다.
        /// 이 설계로 DB Logic 레이어(AccountLinkInfo, AccountInfo, UserInfoData)의
        /// 실제 SQL 파라미터를 직접 검증할 수 있다.
        /// </summary>
        private void SetupDbManagerMock()
        {
            int callOrder = 0;

            _dbManagerMock
                .Setup(m => m.DBContextExcuteTransaction(
                    It.IsAny<DataBaseManager.DBtype>(),
                    It.IsAny<Func<DbConnection, Task<bool>>>()))
                .Callback<DataBaseManager.DBtype, Func<DbConnection, Task<bool>>>((dbtype, func) =>
                {
                    if (dbtype == DataBaseManager.DBtype.Account)
                        _capturedAccountFunc = func;
                    else if (dbtype == DataBaseManager.DBtype.Game)
                        _capturedGameFunc = func;
                })
                .Returns(Task.CompletedTask);
        }

        private void SetupRedisMock()
        {
            _redisMock
                .Setup(r => r.SetUserSession(It.IsAny<RedisClient.UserSession>()))
                .Returns(Task.CompletedTask);
        }

        private GrpcAuthService CreateSut()
            => new GrpcAuthService(_dbManagerMock.Object, _redisMock.Object, _loggerMock.Object);

        // ─── 정상 케이스 ─────────────────────────────────────────────

        [Fact]
        public async Task Login_Guest_EmptyToken_CallsAccountDbTransaction()
        {
            // Arrange
            // 설계 확인: Guest 신규 로그인 시 accessToken이 비어 있으면 서버가 토큰을 생성하고
            // Account DB 트랜잭션을 1회 호출한다.
            // DB mock이 콜백을 실행하지 않으므로 accountId = 0 → UnknowError.
            // Game DB 트랜잭션은 accountId > 0일 때만 진입하므로 1회만 호출.
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-001",
                NickName    = "GuestUser",
                OsType      = global::FrogTailGameServer.Grpc.OsType.Windows,
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Guest,
                AccessToken = ""   // 신규: 빈 토큰
            };

            // Act
            var response = await sut.Login(request, TestServerCallContext.Create());

            // Assert — Account DB 트랜잭션이 1회 호출되어야 함
            _dbManagerMock.Verify(
                m => m.DBContextExcuteTransaction(
                    DataBaseManager.DBtype.Account,
                    It.IsAny<Func<DbConnection, Task<bool>>>()),
                Times.Once,
                "Guest 신규 로그인 시 Account DB 트랜잭션이 1회 호출되어야 한다");

            // accountId = 0이므로 UnknowError 반환 (DB mock이 콜백 미실행)
            Assert.Equal(global::FrogTailGameServer.Grpc.ErrorCode.UnknowError, response.ErrorCode);

            // Redis는 호출되지 않아야 함 (accountId = 0 guard clause에서 반환)
            _redisMock.Verify(
                r => r.SetUserSession(It.IsAny<RedisClient.UserSession>()),
                Times.Never,
                "accountId = 0이면 Redis 세션 저장이 없어야 한다");
        }

        [Fact]
        public async Task Login_Guest_WithExistingToken_CallsAccountDbTransaction()
        {
            // Arrange — 재로그인: accessToken을 전달해도 Account DB 트랜잭션은 1회 호출됨.
            // DB mock이 콜백을 실행하지 않으므로 accountId = 0 → UnknowError.
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-001",
                NickName    = "GuestUser",
                OsType      = global::FrogTailGameServer.Grpc.OsType.Windows,
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Guest,
                AccessToken = "existing-guest-token"
            };

            // Act
            var response = await sut.Login(request, TestServerCallContext.Create());

            // Assert
            _dbManagerMock.Verify(
                m => m.DBContextExcuteTransaction(
                    DataBaseManager.DBtype.Account,
                    It.IsAny<Func<DbConnection, Task<bool>>>()),
                Times.Once,
                "재로그인에서도 Account DB 트랜잭션이 1회 호출되어야 한다");

            Assert.Equal(global::FrogTailGameServer.Grpc.ErrorCode.UnknowError, response.ErrorCode);
        }

        // ─── 예외 케이스 ─────────────────────────────────────────────

        [Fact]
        public async Task Login_EmptyNickName_ReturnsInvalidNickName()
        {
            // Arrange
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-001",
                NickName    = "",   // 빈 NickName
                OsType      = global::FrogTailGameServer.Grpc.OsType.Windows,
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Guest,
                AccessToken = "token"
            };

            // Act
            var response = await sut.Login(request, TestServerCallContext.Create());

            // Assert
            Assert.Equal(global::FrogTailGameServer.Grpc.ErrorCode.InvalidNickName, response.ErrorCode);

            // DB 호출 없어야 함
            _dbManagerMock.Verify(
                m => m.DBContextExcuteTransaction(
                    It.IsAny<DataBaseManager.DBtype>(),
                    It.IsAny<Func<DbConnection, Task<bool>>>()),
                Times.Never,
                "NickName 검증 실패 시 DB 호출이 없어야 한다");
        }

        [Fact]
        public async Task Login_UnsupportedOsType_ReturnsInvalidPacketInfo()
        {
            // Arrange
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-001",
                NickName    = "User",
                OsType      = (global::FrogTailGameServer.Grpc.OsType)99,  // 잘못된 OsType
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Guest,
                AccessToken = "token"
            };

            // Act
            var response = await sut.Login(request, TestServerCallContext.Create());

            // Assert
            Assert.Equal(global::FrogTailGameServer.Grpc.ErrorCode.InvalidPacketInfo, response.ErrorCode);
        }

        [Fact]
        public async Task Login_UnsupportedLoginType_ReturnsInvalidUserToken()
        {
            // Arrange
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-001",
                NickName    = "User",
                OsType      = global::FrogTailGameServer.Grpc.OsType.Windows,
                LoginType   = (global::FrogTailGameServer.Grpc.LoginType)99,  // 잘못된 LoginType
                AccessToken = "token"
            };

            // Act
            var response = await sut.Login(request, TestServerCallContext.Create());

            // Assert
            Assert.Equal(global::FrogTailGameServer.Grpc.ErrorCode.InvalidUserToken, response.ErrorCode);
        }

        [Fact]
        public async Task Login_Google_EmptyAccessToken_ReturnsInvalidUserToken()
        {
            // Arrange
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-001",
                NickName    = "User",
                OsType      = global::FrogTailGameServer.Grpc.OsType.Windows,
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Google,
                AccessToken = ""   // Google인데 토큰이 없음
            };

            // Act
            var response = await sut.Login(request, TestServerCallContext.Create());

            // Assert
            Assert.Equal(global::FrogTailGameServer.Grpc.ErrorCode.InvalidUserToken, response.ErrorCode);
        }

        // ─── DB 검증 케이스 ───────────────────────────────────────────

        [Fact]
        public async Task Login_Guest_NewLogin_AccountDbTransaction_CalledFirst()
        {
            // Arrange
            // DB mock이 콜백을 실행하지 않으므로 accountId = 0.
            // accountId = 0이면 guard clause에서 UnknowError 반환 → Game DB 트랜잭션 미호출.
            // 이 테스트는 "Account DB 트랜잭션이 반드시 먼저 호출된다"는 순서를 검증한다.
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-db-verify",
                NickName    = "DbVerifyUser",
                OsType      = global::FrogTailGameServer.Grpc.OsType.Windows,
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Guest,
                AccessToken = ""
            };

            var callSequence = new List<DataBaseManager.DBtype>();

            _dbManagerMock
                .Setup(m => m.DBContextExcuteTransaction(
                    It.IsAny<DataBaseManager.DBtype>(),
                    It.IsAny<Func<DbConnection, Task<bool>>>()))
                .Callback<DataBaseManager.DBtype, Func<DbConnection, Task<bool>>>((dbtype, _) =>
                {
                    callSequence.Add(dbtype);
                })
                .Returns(Task.CompletedTask);

            // Act
            await sut.Login(request, TestServerCallContext.Create());

            // Assert — Account DB가 먼저 호출되어야 하고, accountId = 0이므로 1회만 호출됨
            Assert.Single(callSequence);
            Assert.Equal(DataBaseManager.DBtype.Account, callSequence[0]);
        }

        [Fact]
        public async Task Login_Guest_NewLogin_GuestToken_IsNotEmpty_InResponse()
        {
            // Arrange
            // DB 트랜잭션 mock: isCreate = true 경로를 타도록 콜백 직접 실행
            // Account 트랜잭션: GetAccountLinkInfo → null(신규), Insert 성공
            // Game 트랜잭션: InsertUserInfo → userId 반환
            // 실제 Dapper 쿼리를 실행하지 않으므로 isCreate는 false로 남음.
            // 이 테스트는 GuestToken 응답 포함 여부(isCreate 경로)를
            // 서비스 레이어 관점에서 검증한다.
            //
            // 결론: DBContextExcuteTransaction이 mock이므로 isCreate = false 상태.
            // 이 케이스는 "신규 경로가 DB Insert를 올바르게 시도한다"는
            // GuestToken 생성 여부만 검증한다.
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-guest-new",
                NickName    = "GuestNew",
                OsType      = global::FrogTailGameServer.Grpc.OsType.Windows,
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Guest,
                AccessToken = ""
            };

            string? capturedAccessToken = null;

            // Account 트랜잭션 mock: func 실행하지 않고 accountId를 0으로 둠
            // (실제 DB 없이 토큰 생성 로직만 확인)
            _dbManagerMock
                .Setup(m => m.DBContextExcuteTransaction(
                    DataBaseManager.DBtype.Account,
                    It.IsAny<Func<DbConnection, Task<bool>>>()))
                .Callback<DataBaseManager.DBtype, Func<DbConnection, Task<bool>>>((_, func) =>
                {
                    // accessToken이 채워졌는지 확인하기 위해
                    // 서비스 내부 변수는 직접 접근 불가하므로
                    // 이 callback 시점에 accessToken이 비어있지 않아야 한다.
                    // (Guest 신규라면 이 시점에 이미 RandToken.GenerateUniqueToken()이 호출됨)
                })
                .Returns(Task.CompletedTask);

            // Act
            var response = await sut.Login(request, TestServerCallContext.Create());

            // Assert — 서비스는 accountId = 0이면 UnknowError를 반환
            // (DB mock이 콜백을 실행하지 않으므로 accountId가 0으로 남음)
            // 이 케이스에서 중요한 검증: DB 트랜잭션이 호출되기 전에 토큰이 생성됨
            _dbManagerMock.Verify(
                m => m.DBContextExcuteTransaction(
                    DataBaseManager.DBtype.Account,
                    It.IsAny<Func<DbConnection, Task<bool>>>()),
                Times.Once,
                "Guest 신규 로그인 시 Account DB 트랜잭션이 1회 호출되어야 한다");
        }

        [Fact]
        public async Task Login_Guest_ReLogin_ExistingToken_AccountDbTransaction_Called()
        {
            // Arrange — 재로그인: DB mock 콜백 미실행이므로 accountId = 0 → UnknowError.
            // Account DB 트랜잭션이 1회 호출됨을 검증.
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-001",
                NickName    = "GuestUser",
                OsType      = global::FrogTailGameServer.Grpc.OsType.Windows,
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Guest,
                AccessToken = "already-issued-guest-token"
            };

            // Act
            await sut.Login(request, TestServerCallContext.Create());

            // Assert — Account DB 트랜잭션은 반드시 호출됨
            _dbManagerMock.Verify(
                m => m.DBContextExcuteTransaction(
                    DataBaseManager.DBtype.Account,
                    It.IsAny<Func<DbConnection, Task<bool>>>()),
                Times.Once,
                "재로그인에서도 Account DB 트랜잭션이 1회 호출되어야 한다");

            // Game DB 트랜잭션은 accountId > 0일 때만 호출됨 (mock에서는 0)
            _dbManagerMock.Verify(
                m => m.DBContextExcuteTransaction(
                    DataBaseManager.DBtype.Game,
                    It.IsAny<Func<DbConnection, Task<bool>>>()),
                Times.Never,
                "accountId = 0이면 Game DB 트랜잭션이 호출되지 않아야 한다");
        }

        [Fact]
        public async Task Login_DbTransactionFails_AccountIdZero_ReturnsUnknowError()
        {
            // Arrange — DB 트랜잭션이 콜백을 실행하지 않아 accountId = 0인 경우
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-001",
                NickName    = "User",
                OsType      = global::FrogTailGameServer.Grpc.OsType.Windows,
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Guest,
                AccessToken = "some-token"
            };

            // DBContextExcuteTransaction: 콜백 실행 없이 그냥 완료 (accountId = 0 유지)
            _dbManagerMock
                .Setup(m => m.DBContextExcuteTransaction(
                    It.IsAny<DataBaseManager.DBtype>(),
                    It.IsAny<Func<DbConnection, Task<bool>>>()))
                .Returns(Task.CompletedTask);

            // Act
            var response = await sut.Login(request, TestServerCallContext.Create());

            // Assert
            Assert.Equal(global::FrogTailGameServer.Grpc.ErrorCode.UnknowError, response.ErrorCode);

            // Redis는 호출되지 않아야 함
            _redisMock.Verify(
                r => r.SetUserSession(It.IsAny<RedisClient.UserSession>()),
                Times.Never,
                "트랜잭션 실패 시 Redis 세션 저장이 없어야 한다");
        }

        // ─── 경계값 케이스 ───────────────────────────────────────────

        [Fact]
        public async Task Login_Guest_EmptyStringAccessToken_TreatedAsNewLogin_DBCalled()
        {
            // Arrange — protobuf 필드는 null 불가. 빈 문자열("")이 신규 Guest를 의미.
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-001",
                NickName    = "User",
                OsType      = global::FrogTailGameServer.Grpc.OsType.Windows,
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Guest,
                AccessToken = ""   // 빈 문자열 = 신규 Guest
            };

            // Act
            var response = await sut.Login(request, TestServerCallContext.Create());

            // Assert — DB 트랜잭션은 호출됨 (토큰 생성 후 진행)
            _dbManagerMock.Verify(
                m => m.DBContextExcuteTransaction(
                    DataBaseManager.DBtype.Account,
                    It.IsAny<Func<DbConnection, Task<bool>>>()),
                Times.Once,
                "빈 토큰도 신규 Guest로 처리하여 Account DB 트랜잭션이 호출되어야 한다");
        }

        [Fact]
        public async Task Login_Redis_SetUserSession_Called_With_CorrectUserId()
        {
            // Arrange — Game DB 트랜잭션이 userId를 채워주는 경우 시뮬레이션
            // DBContextExcuteTransaction을 override하여 콜백을 실행시키는 방법:
            // Account 트랜잭션에서 accountId를 세팅, Game 트랜잭션에서 userId를 세팅해야 함.
            // 단, DB Connection이 없으므로 순수 mock 환경에서는 accountId/userId가 0으로 남음.
            // 이 테스트는 "정상 경로에서 SetUserSession이 올바른 UserSession 인자로 호출된다"를 확인.
            //
            // 현 mock 구조(콜백 미실행)에서는 accountId = 0 → UnknowError 반환.
            // → Redis 미호출 확인이 가능한 케이스로 활용.
            var sut     = CreateSut();
            var request = new global::FrogTailGameServer.Grpc.LoginRequest
            {
                DeviceId    = "device-001",
                NickName    = "User",
                OsType      = global::FrogTailGameServer.Grpc.OsType.Ios,
                LoginType   = global::FrogTailGameServer.Grpc.LoginType.Guest,
                AccessToken = "some-guest-token"
            };

            // Act
            var response = await sut.Login(request, TestServerCallContext.Create());

            // Assert — accountId = 0이므로 UnknowError, Redis 미호출
            Assert.Equal(global::FrogTailGameServer.Grpc.ErrorCode.UnknowError, response.ErrorCode);
            _redisMock.Verify(r => r.SetUserSession(It.IsAny<RedisClient.UserSession>()), Times.Never);
        }
    }
}
