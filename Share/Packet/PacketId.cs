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
		CG_VerityLogin_Req_Packet_Id = 3,
		GC_VerityLogin_Ans_Packet_Id = 4,


		CG_ShopList_Req_Packet_Id = 100,
		GC_ShopList_Ans_Packet_Id = 101,

		// 계정 연동
		CG_AccountLinkAdd_Req_Packet_Id    = 200,
		GC_AccountLinkAdd_Ans_Packet_Id    = 201,
		CG_AccountLinkRemove_Req_Packet_Id = 202,
		GC_AccountLinkRemove_Ans_Packet_Id = 203,
		CG_AccountLinkList_Req_Packet_Id   = 204,
		GC_AccountLinkList_Ans_Packet_Id   = 205,

		// 게임 룸
		CG_GameReady_Req_Packet_Id = 300,
		GC_GameReady_Ans_Packet_Id = 301,
		GC_GameStart_Notify_Packet_Id = 302,
		CG_GameAction_Req_Packet_Id = 303,
		GC_GameAction_Ans_Packet_Id = 304,
		GC_GameState_Notify_Packet_Id = 305,
		GC_GameEnd_Notify_Packet_Id = 306,
		CG_GameLeave_Req_Packet_Id = 307,
		GC_GameLeave_Ans_Packet_Id = 308,
	}
}
