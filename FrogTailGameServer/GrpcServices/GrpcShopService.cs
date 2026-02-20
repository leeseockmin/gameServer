using DB;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace FrogTailGameServer.GrpcServices
{
    public class GrpcShopService : global::FrogTailGameServer.Grpc.ShopService.ShopServiceBase
    {
        private readonly DataBaseManager _dataBaseManager;
        private readonly ILogger<GrpcShopService> _logger;

        public GrpcShopService(DataBaseManager dataBaseManager, ILogger<GrpcShopService> logger)
        {
            _dataBaseManager = dataBaseManager;
            _logger = logger;
        }

        public override Task<global::FrogTailGameServer.Grpc.ShopListResponse> ShopList(
            global::FrogTailGameServer.Grpc.ShopListRequest request, ServerCallContext context)
        {
            var response = new global::FrogTailGameServer.Grpc.ShopListResponse
            {
                ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.Success
            };

            // TODO: 상점 목록 조회 로직 구현
            // await _dataBaseManager.DBContextExecute(DataBaseManager.DBtype.Game, async (conn) =>
            // {
            //     var shopList = await DB.Data.Logic.GameDBLogic.ShopData.ShopList(conn);
            //     response.ShopDatas.AddRange(shopList.Select(...));
            // });

            _logger.LogInformation("[gRPC ShopList] called.");
            return Task.FromResult(response);
        }
    }
}
