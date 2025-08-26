using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Packet
{
	public enum PacketId
	{
		None = 0,
		CG_Login_Req_Packet_Id = 1,
		GC_Login_Ans_Packet_Id = 2,
		CG_ShopList_Req_Packet_Id = 3,
		GC_ShopList_Ans_Packet_Id = 4,

	}
}
