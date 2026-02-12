using DB;
using Serilog;
using Share.Common;
using Share.Packet.ShopPacket;

namespace FrogTailGameServer.Services
{
	public class ShopService
	{
		private readonly DataBaseManager _dataBaseManager;

		public ShopService(DataBaseManager dataBaseManager)
		{
			_dataBaseManager = dataBaseManager;
		}

		public async Task<GCShopListAnsPacket> GetShopListAsync()
		{
			var ans = new GCShopListAnsPacket();

			// TODO: 상점 목록 조회 로직 구현
			ans.ShopDatas = new List<ShopData>();

			return ans;
		}
	}
}
