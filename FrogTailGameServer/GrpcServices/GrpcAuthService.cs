using Common.Redis;
using DataBase.AccountDB;
using DataBase.GameDB;
using DB;
using FrogTailGameServer.Logic.Utils;
using Grpc.Core;
using GameServer.Logic.Utils;
using Microsoft.Extensions.Logging;
using Share.Common;

namespace FrogTailGameServer.GrpcServices
{
    public class GrpcAuthService : global::FrogTailGameServer.Grpc.LoginService.LoginServiceBase
    {
        private readonly DataBaseManager _dataBaseManager;
        private readonly RedisClient _redisClient;
        private readonly ILogger<GrpcAuthService> _logger;

        public GrpcAuthService(
            DataBaseManager dataBaseManager,
            RedisClient redisClient,
            ILogger<GrpcAuthService> logger)
        {
            _dataBaseManager = dataBaseManager;
            _redisClient = redisClient;
            _logger = logger;
        }

        public override async Task<global::FrogTailGameServer.Grpc.VerityLoginResponse> VerityLogin(
            global::FrogTailGameServer.Grpc.VerityLoginRequest request, ServerCallContext context)
        {
            var loginType = (LoginType)(int)request.LoginType;
            var accessToken = request.AccessToken;

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("[gRPC VerifyLogin] AccessToken is empty or null");
                return new global::FrogTailGameServer.Grpc.VerityLoginResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidUserToken
                };
            }

            switch (loginType)
            {
                case LoginType.Guest:
                    break;
                case LoginType.Google:
                case LoginType.Apple:
                case LoginType.Email:
                {
                    var resolvedLoginType = await FireBase.GetLoginProviderAsync(accessToken);
                    if (resolvedLoginType != loginType)
                    {
                        _logger.LogError("[gRPC VerifyLogin] Invalid LoginType: {LoginType}", loginType);
                        return new global::FrogTailGameServer.Grpc.VerityLoginResponse
                        {
                            ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidPacketInfo
                        };
                    }
                    break;
                }
                default:
                    _logger.LogError("[gRPC VerifyLogin] Unsupported LoginType: {LoginType}", loginType);
                    return new global::FrogTailGameServer.Grpc.VerityLoginResponse
                    {
                        ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidUserToken
                    };
            }

            return new global::FrogTailGameServer.Grpc.VerityLoginResponse
            {
                ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.Success
            };
        }

        public override async Task<global::FrogTailGameServer.Grpc.LoginResponse> Login(
            global::FrogTailGameServer.Grpc.LoginRequest request, ServerCallContext context)
        {
            var loginType = (LoginType)(int)request.LoginType;
            var osType = (OsType)(int)request.OsType;
            var accessToken = request.AccessToken;
            var nickName = request.NickName;
            var deviceId = request.DeviceId;

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("[gRPC Login] AccessToken is empty or null");
                return new global::FrogTailGameServer.Grpc.LoginResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidUserToken
                };
            }

            if (string.IsNullOrEmpty(nickName))
            {
                _logger.LogError("[gRPC Login] NickName is empty or null");
                return new global::FrogTailGameServer.Grpc.LoginResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidNickName
                };
            }

            switch (osType)
            {
                case OsType.AOS:
                case OsType.IOS:
                case OsType.Windows:
                    break;
                default:
                    _logger.LogError("[gRPC Login] Unsupported OsType: {OsType}", osType);
                    return new global::FrogTailGameServer.Grpc.LoginResponse
                    {
                        ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidPacketInfo
                    };
            }

            switch (loginType)
            {
                case LoginType.Guest:
                    break;
                case LoginType.Google:
                case LoginType.Apple:
                {
                    var resolvedLoginType = await FireBase.GetLoginProviderAsync(accessToken);
                    if (resolvedLoginType != loginType)
                    {
                        _logger.LogError("[gRPC Login] Firebase LoginType mismatch. Expected: {Expected}, Got: {Got}", loginType, resolvedLoginType);
                        return new global::FrogTailGameServer.Grpc.LoginResponse
                        {
                            ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidPacketInfo
                        };
                    }
                    break;
                }
                default:
                    _logger.LogError("[gRPC Login] Unsupported LoginType: {LoginType}", loginType);
                    return new global::FrogTailGameServer.Grpc.LoginResponse
                    {
                        ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidUserToken
                    };
            }

            DateTime now = DateTime.UtcNow;
            bool isCreate = false;
            long accountId = 0;

            await _dataBaseManager.DBContextExecuteTransaction(DataBaseManager.DBtype.Account, async (accountDBConnection) =>
            {
                var getAccountLinkInfo = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.GetAccountLinkInfo(
                    accountDBConnection, loginType, accessToken);

                if (getAccountLinkInfo == null)
                {
                    var newAccount = new Account
                    {
                        osType = osType,
                        deviceId = deviceId,
                        loginType = loginType,
                        updateDate = now
                    };

                    long lastAccountId = await DB.Data.Logic.AccountDBLogic.AccountInfo.InsertAccountInfo(
                        accountDBConnection, newAccount);

                    if (lastAccountId <= 0)
                    {
                        _logger.LogError("[gRPC Login] InsertAccountInfo failed.");
                        return false;
                    }

                    var newAccountLink = new AccountLink
                    {
                        loginType = loginType,
                        accessToken = accessToken,
                        createDate = now,
                        accountId = lastAccountId
                    };

                    int affectedCnt = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.InsertAccountLinkInfo(
                        accountDBConnection, newAccountLink);

                    if (affectedCnt <= 0)
                    {
                        _logger.LogError("[gRPC Login] InsertAccountLinkInfo failed.");
                        return false;
                    }

                    accountId = lastAccountId;
                    isCreate = true;
                }
                else
                {
                    var getAccountInfo = await DB.Data.Logic.AccountDBLogic.AccountInfo.GetAccountInfo(
                        accountDBConnection, getAccountLinkInfo.accountId);

                    if (getAccountInfo == null)
                    {
                        _logger.LogError("[gRPC Login] GetAccountInfo returned null for accountId: {AccountId}", getAccountLinkInfo.accountId);
                        return false;
                    }

                    getAccountInfo.osType = osType;
                    getAccountInfo.loginType = loginType;
                    getAccountInfo.updateDate = now;
                    getAccountInfo.lastLoginTime = now;

                    int affectedCnt = await DB.Data.Logic.AccountDBLogic.AccountInfo.UpdateLoginAccountInfo(
                        accountDBConnection, getAccountInfo);

                    if (affectedCnt <= 0)
                    {
                        _logger.LogError("[gRPC Login] UpdateLoginAccountInfo failed for accountId: {AccountId}", getAccountInfo.accountId);
                        return false;
                    }

                    accountId = getAccountInfo.accountId;
                }

                return true;
            });

            if (accountId <= 0)
            {
                _logger.LogError("[gRPC Login] Account transaction failed. accountId is 0.");
                return new global::FrogTailGameServer.Grpc.LoginResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.UnknowError
                };
            }

            long userId = 0;

            await _dataBaseManager.DBContextExecuteTransaction(DataBaseManager.DBtype.Game, async (gameDBConnection) =>
            {
                if (isCreate)
                {
                    var newUserInfo = new UserInfo
                    {
                        nickName = nickName,
                        accountId = accountId
                    };

                    long lastUserId = await DB.Data.Logic.GameDBLogic.UserInfoData.InsertUserInfo(
                        gameDBConnection, newUserInfo);

                    if (lastUserId <= 0)
                    {
                        _logger.LogError("[gRPC Login] InsertUserInfo failed for accountId: {AccountId}", accountId);
                        return false;
                    }

                    userId = lastUserId;
                }
                else
                {
                    var userInfo = await DB.Data.Logic.GameDBLogic.UserInfoData.GetUserInfoByAccountId(
                        gameDBConnection, accountId);

                    if (userInfo == null)
                    {
                        _logger.LogError("[gRPC Login] GetUserInfoByAccountId returned null for accountId: {AccountId}", accountId);
                        return false;
                    }

                    userId = userInfo.userId;
                }

                return true;
            });

            if (userId <= 0)
            {
                _logger.LogError("[gRPC Login] Game DB transaction failed. userId is 0.");
                return new global::FrogTailGameServer.Grpc.LoginResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.UnknowError
                };
            }

            var userSession = new RedisClient.UserSession
            {
                userId = userId,
                userToken = RandToken.GenerateUniqueToken()
            };

            await _redisClient.SetUserSession(userSession);

            _logger.LogInformation("[gRPC Login] Login success. UserId: {UserId}", userId);

            return new global::FrogTailGameServer.Grpc.LoginResponse
            {
                ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.Success,
                UserToken = userSession.userToken,
                UserId = userSession.userId
            };
        }
    }
}
