using Share.Common;

namespace Share.Packet
{
	public class AccountLinkInfoDto
	{
		public long LinkId { get; set; }
		public LoginType LoginType { get; set; }
		public DateTime CreateDate { get; set; }
	}

	public class CGAccountLinkAddReqPacket : PacketRequestBase
	{
		public CGAccountLinkAddReqPacket() : base(PacketId.CG_AccountLinkAdd_Req_Packet_Id)
		{
		}
		public LoginType LinkLoginType { get; set; }
		public string AccessToken { get; set; }
	}

	public class GCAccountLinkAddAnsPacket : PacketAnsPacket
	{
		public GCAccountLinkAddAnsPacket()
		{
			ErrorCode = Share.Common.ErrorCode.SUCCESS;
		}
		public List<AccountLinkInfoDto> LinkedAccounts { get; set; } = new();
	}

	public class CGAccountLinkRemoveReqPacket : PacketRequestBase
	{
		public CGAccountLinkRemoveReqPacket() : base(PacketId.CG_AccountLinkRemove_Req_Packet_Id)
		{
		}
		public long LinkId { get; set; }
	}

	public class GCAccountLinkRemoveAnsPacket : PacketAnsPacket
	{
		public GCAccountLinkRemoveAnsPacket()
		{
			ErrorCode = Share.Common.ErrorCode.SUCCESS;
		}
		public List<AccountLinkInfoDto> LinkedAccounts { get; set; } = new();
	}

	public class CGAccountLinkListReqPacket : PacketRequestBase
	{
		public CGAccountLinkListReqPacket() : base(PacketId.CG_AccountLinkList_Req_Packet_Id)
		{
		}
	}

	public class GCAccountLinkListAnsPacket : PacketAnsPacket
	{
		public GCAccountLinkListAnsPacket()
		{
			ErrorCode = Share.Common.ErrorCode.SUCCESS;
		}
		public List<AccountLinkInfoDto> LinkedAccounts { get; set; } = new();
	}
}
