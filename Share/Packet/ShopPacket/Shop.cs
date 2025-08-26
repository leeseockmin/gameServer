using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Packet.ShopPacket
{
	public class ShopData
	{
		public int ShopId { get; set; }
		public List<ShopItemData> ShopItemDatas { get;set;}
	}
	public class ShopItemData
	{
		public int ShopItemId { get; set; }
		public long BuyCount { get; set; }

	}
	public class CGShopListReqPacket : PacketReqeustBase
	{
		public CGShopListReqPacket() : base(PacketId.CG_ShopList_Req_Packet_Id)
		{

		}
	}
	public class GCShopListAnsPacket : PacketAnsPacket
	{
		public GCShopListAnsPacket()
		{
			ErrorCode = Common.ErrrorCode.SUCCESS;
		}
		public List<ShopData> ShopDatas { get; set; }
	}
}
