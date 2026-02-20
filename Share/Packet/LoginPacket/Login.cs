using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Packet
{
	public class CGLoginReqPacket : PacketRequestBase
	{
		public CGLoginReqPacket() : base(PacketId.CG_Login_Req_Packet_Id)
		{

		}
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


	public class CGVerityLoginReqPacket : PacketRequestBase
	{

		public CGVerityLoginReqPacket() : base(PacketId.CG_VerityLogin_Req_Packet_Id)
		{

		}
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

}
