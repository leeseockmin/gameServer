using Common.Redis;
using DataBase.AccountDB;
using DB;
using FrogTailGameServer.MiddleWare.AccountLink;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Share.Common;

namespace FrogTailGameServer.GrpcServices
{
    public class GrpcAccountLinkService : global::FrogTailGameServer.Grpc.AccountLinkService.AccountLinkServiceBase
    {
        private readonly DataBaseManager _dataBaseManager;
        private readonly RedisClient _redisClient;
        private readonly IAccountLinkValidator _validator;
        private readonly ILogger<GrpcAccountLinkService> _logger;

        public GrpcAccountLinkService(
            DataBaseManager dataBaseManager,
            RedisClient redisClient,
            IAccountLinkValidator validator,
            ILogger<GrpcAccountLinkService> logger)
        {
            _dataBaseManager = dataBaseManager;
            _redisClient = redisClient;
            _validator = validator;
            _logger = logger;
        }

        // x-userid 헤더(userId) → GameDB 조회 → accountId 반환
        private async Task<long> GetAccountIdAsync(ServerCallContext context)
        {
            var userIdEntry = context.RequestHeaders.FirstOrDefault(e => e.Key == "x-userid")?.Value;
            if (string.IsNullOrEmpty(userIdEntry) || !long.TryParse(userIdEntry, out long userId))
            {
                return 0;
            }

            long accountId = 0;
            await _dataBaseManager.DBContextExecute(DataBaseManager.DBtype.Game, async (gameConn) =>
            {
                var userInfo = await DB.Data.Logic.GameDBLogic.UserInfoData.GetUserInfoByUserId(gameConn, userId).ConfigureAwait(false);
                if (userInfo != null)
                {
                    accountId = userInfo.accountId;
                }
            }).ConfigureAwait(false);

            return accountId;
        }

        private static List<global::FrogTailGameServer.Grpc.AccountLinkInfoProto> ToProtoList(List<AccountLink> links)
        {
            return links.Select(l => new global::FrogTailGameServer.Grpc.AccountLinkInfoProto
            {
                LinkId    = l.id,
                LoginType = (global::FrogTailGameServer.Grpc.LoginType)(int)l.loginType,
                CreateDate = l.createDate.ToString("o")
            }).ToList();
        }

        public override async Task<global::FrogTailGameServer.Grpc.AccountLinkAddResponse> AccountLinkAdd(
            global::FrogTailGameServer.Grpc.AccountLinkAddRequest request, ServerCallContext context)
        {
            var linkLoginType = (LoginType)(int)request.LinkLoginType;
            var accessToken   = request.AccessToken;

            // 1. Guest/None 타입은 연동 불가
            if (linkLoginType == LoginType.Guest || linkLoginType == LoginType.None)
            {
                _logger.LogWarning("[gRPC AccountLinkAdd] Unsupported LoginType: {LoginType}", linkLoginType);
                return new global::FrogTailGameServer.Grpc.AccountLinkAddResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.AccountLinkUnsupportedType
                };
            }

            // 2. 토큰 유효성 검사
            bool isValid = await _validator.ValidateAsync(linkLoginType, accessToken).ConfigureAwait(false);
            if (!isValid)
            {
                _logger.LogWarning("[gRPC AccountLinkAdd] Invalid accessToken. LoginType: {LoginType}", linkLoginType);
                return new global::FrogTailGameServer.Grpc.AccountLinkAddResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.AccountLinkInvalidKey
                };
            }

            long accountId = await GetAccountIdAsync(context).ConfigureAwait(false);
            if (accountId <= 0)
            {
                _logger.LogError("[gRPC AccountLinkAdd] Failed to resolve accountId from session.");
                return new global::FrogTailGameServer.Grpc.AccountLinkAddResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidUserToken
                };
            }

            global::FrogTailGameServer.Grpc.AccountLinkAddResponse? response = null;

            await _dataBaseManager.DBContextExecuteTransaction(DataBaseManager.DBtype.Account, async (accountConn) =>
            {
                // 3. 이미 해당 loginType으로 연동되어 있는지 확인
                var existingByType = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.GetAccountLinkByLoginType(
                    accountConn, accountId, linkLoginType).ConfigureAwait(false);

                if (existingByType != null)
                {
                    _logger.LogWarning("[gRPC AccountLinkAdd] LoginType already linked. AccountId: {AccountId}, LoginType: {LoginType}", accountId, linkLoginType);
                    response = new global::FrogTailGameServer.Grpc.AccountLinkAddResponse
                    {
                        ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.AccountLinkAlreadyLinked
                    };
                    return false;
                }

                // 4. 다른 계정이 이 accessToken을 사용 중인지 확인
                var existingByToken = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.GetAccountLinkInfo(
                    accountConn, linkLoginType, accessToken).ConfigureAwait(false);

                if (existingByToken != null)
                {
                    _logger.LogWarning("[gRPC AccountLinkAdd] AccessToken already used by another account. AccountId: {AccountId}", existingByToken.accountId);
                    response = new global::FrogTailGameServer.Grpc.AccountLinkAddResponse
                    {
                        ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.AccountLinkAlreadyLinked
                    };
                    return false;
                }

                // 5. 연동 추가
                var newLink = new AccountLink
                {
                    loginType   = linkLoginType,
                    accessToken = accessToken,
                    accountId   = accountId,
                    createDate  = DateTime.UtcNow
                };

                int affected = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.InsertAccountLinkInfo(
                    accountConn, newLink).ConfigureAwait(false);

                if (affected <= 0)
                {
                    _logger.LogError("[gRPC AccountLinkAdd] InsertAccountLinkInfo failed. AccountId: {AccountId}", accountId);
                    response = new global::FrogTailGameServer.Grpc.AccountLinkAddResponse
                    {
                        ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.UnknowError
                    };
                    return false;
                }

                // 6. 전체 연동 목록 반환
                var links = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.GetAccountLinkInfos(
                    accountConn, accountId).ConfigureAwait(false);

                var resp = new global::FrogTailGameServer.Grpc.AccountLinkAddResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.Success
                };
                resp.LinkedAccounts.AddRange(ToProtoList(links));
                response = resp;
                return true;

            }).ConfigureAwait(false);

            return response ?? new global::FrogTailGameServer.Grpc.AccountLinkAddResponse
            {
                ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.UnknowError
            };
        }

        public override async Task<global::FrogTailGameServer.Grpc.AccountLinkRemoveResponse> AccountLinkRemove(
            global::FrogTailGameServer.Grpc.AccountLinkRemoveRequest request, ServerCallContext context)
        {
            long accountId = await GetAccountIdAsync(context).ConfigureAwait(false);
            if (accountId <= 0)
            {
                _logger.LogError("[gRPC AccountLinkRemove] Failed to resolve accountId from session.");
                return new global::FrogTailGameServer.Grpc.AccountLinkRemoveResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidUserToken
                };
            }

            global::FrogTailGameServer.Grpc.AccountLinkRemoveResponse? response = null;

            await _dataBaseManager.DBContextExecuteTransaction(DataBaseManager.DBtype.Account, async (accountConn) =>
            {
                // 1. 현재 연동 목록 조회 — 1개 이하면 마지막 연동이므로 삭제 불가
                var links = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.GetAccountLinkInfos(
                    accountConn, accountId).ConfigureAwait(false);

                if (links.Count <= 1)
                {
                    _logger.LogWarning("[gRPC AccountLinkRemove] Cannot remove last link. AccountId: {AccountId}", accountId);
                    response = new global::FrogTailGameServer.Grpc.AccountLinkRemoveResponse
                    {
                        ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.AccountLinkLastLink
                    };
                    return false;
                }

                // 2. 삭제 — linkId + accountId 소유 확인 후 삭제
                int affected = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.DeleteAccountLinkInfo(
                    accountConn, request.LinkId, accountId).ConfigureAwait(false);

                if (affected <= 0)
                {
                    _logger.LogWarning("[gRPC AccountLinkRemove] LinkId not found or not owned. LinkId: {LinkId}, AccountId: {AccountId}", request.LinkId, accountId);
                    response = new global::FrogTailGameServer.Grpc.AccountLinkRemoveResponse
                    {
                        ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.AccountLinkNotFound
                    };
                    return false;
                }

                // 3. 남은 목록 반환
                var remaining = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.GetAccountLinkInfos(
                    accountConn, accountId).ConfigureAwait(false);

                var resp = new global::FrogTailGameServer.Grpc.AccountLinkRemoveResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.Success
                };
                resp.LinkedAccounts.AddRange(ToProtoList(remaining));
                response = resp;
                return true;

            }).ConfigureAwait(false);

            return response ?? new global::FrogTailGameServer.Grpc.AccountLinkRemoveResponse
            {
                ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.UnknowError
            };
        }

        public override async Task<global::FrogTailGameServer.Grpc.AccountLinkListResponse> AccountLinkList(
            global::FrogTailGameServer.Grpc.AccountLinkListRequest request, ServerCallContext context)
        {
            long accountId = await GetAccountIdAsync(context).ConfigureAwait(false);
            if (accountId <= 0)
            {
                _logger.LogError("[gRPC AccountLinkList] Failed to resolve accountId from session.");
                return new global::FrogTailGameServer.Grpc.AccountLinkListResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.InvalidUserToken
                };
            }

            var response = new global::FrogTailGameServer.Grpc.AccountLinkListResponse
            {
                ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.Success
            };

            await _dataBaseManager.DBContextExecute(DataBaseManager.DBtype.Account, async (accountConn) =>
            {
                var links = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.GetAccountLinkInfos(
                    accountConn, accountId).ConfigureAwait(false);

                response.LinkedAccounts.AddRange(ToProtoList(links));

            }).ConfigureAwait(false);

            return response;
        }
    }
}
