using Grpc.Core;
using GameServer.GameTable;

namespace FrogTailGameServer.GrpcServices
{
    public class GrpcShopService : global::FrogTailGameServer.Grpc.ShopService.ShopServiceBase
    {
        private readonly GameTableManager _gameTableManager;
        private readonly ILogger<GrpcShopService> _logger;

        public GrpcShopService(
            GameTableManager gameTableManager,
            ILogger<GrpcShopService> logger)
        {
            _gameTableManager = gameTableManager;
            _logger = logger;
        }

        public override Task<global::FrogTailGameServer.Grpc.ShopListResponse> ShopList(
            global::FrogTailGameServer.Grpc.ShopListRequest request, ServerCallContext context)
        {
            try
            {
                // 현재는 ItemTable 기반으로 단일 Shop 데이터를 구성합니다.
                // Shop DB 테이블이 추가되면 Dapper 쿼리로 교체합니다.
                var shopData = new global::FrogTailGameServer.Grpc.ShopData
                {
                    ShopId = 1
                };

                var itemTable = _gameTableManager.ItemTableManager;

                // ItemTable의 모든 아이템을 ShopItemData로 변환
                // (현재 ShopItemId = ItemId, BuyCount = 0 초기값)
                var item = itemTable.GetItemTable(1);
                if (item != null)
                {
                    shopData.ShopItemDatas.Add(new global::FrogTailGameServer.Grpc.ShopItemData
                    {
                        ShopItemId = item.ItemId,
                        BuyCount   = 0
                    });
                }

                var response = new global::FrogTailGameServer.Grpc.ShopListResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.Success
                };
                response.ShopDatas.Add(shopData);

                _logger.LogInformation("[gRPC ShopList] Success. ShopCount: {Count}", response.ShopDatas.Count);
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[gRPC ShopList] Unexpected error.");
                return Task.FromResult(new global::FrogTailGameServer.Grpc.ShopListResponse
                {
                    ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.UnknowError
                });
            }
        }
    }
}
