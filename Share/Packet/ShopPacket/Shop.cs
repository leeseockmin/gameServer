using Share.Common;

namespace Share.Packet.ShopPacket
{
	public class ShopData
	{
		public int ShopId { get; set; }
		public List<ShopItemData> ShopItemDatas { get; set; }
	}

	public class ShopItemData
	{
		public int ShopItemId { get; set; }
		public long BuyCount { get; set; }
	}

	public class CGShopListReqPacket : PackettBase
	{
	}

	public class GCShopListAnsPacket : PacketAnsPacket
	{
		public GCShopListAnsPacket()
		{
			ErrorCode = ErrorCode.SUCCESS;
		}
		public List<ShopData> ShopDatas { get; set; }
	}

	public class CGShopBuyReqPacket : PackettBase
	{
		public int ShopItemId { get; set; }
	}

	public class GCShopBuyAnsPacket : PacketAnsPacket
	{
		public GCShopBuyAnsPacket()
		{
			ErrorCode = ErrorCode.SUCCESS;
		}
	}
}
