using Share.Common;

namespace Share.Packet
{
	public class CGLoginReqPacket : PackettBase
	{
		public string DeviceId { get; set; }
		public string NickName { get; set; }
		public OsType OsType { get; set; }
		public LoginType LoginType { get; set; }
		public string AccessToken { get; set; }
	}

	public class GCLoginAnsPacket : PacketAnsPacket
	{
		public GCLoginAnsPacket()
		{
			ErrorCode = Share.Common.ErrorCode.SUCCESS;
		}
		public string UserToken { get; set; }
		public long UserId { get; set; }
	}

	public class CGVerityLoginReqPacket : PackettBase
	{
		public OsType OsType { get; set; }
		public LoginType LoginType { get; set; }
		public string AccessToken { get; set; }
	}

	public class GCVerityLoginAnsPacket : PacketAnsPacket
	{
		public GCVerityLoginAnsPacket()
		{
			ErrorCode = Share.Common.ErrorCode.SUCCESS;
		}
	}

	public class CGAccountLinkReqPacket : PackettBase
	{
		public OsType OsType { get; set; }
		public LoginType LoginType { get; set; }
		public string AccessToken { get; set; }
	}

	public class GCAccountLinkAnsPacket : PacketAnsPacket
	{
		public GCAccountLinkAnsPacket()
		{
			ErrorCode = Share.Common.ErrorCode.SUCCESS;
		}
	}
}
