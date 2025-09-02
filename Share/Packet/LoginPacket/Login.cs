using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Packet
{
	public class CGLoginReqPacket : PacketReqeustBase
	{
		public CGLoginReqPacket() : base(PacketId.CG_Login_Req_Packet_Id)
		{

		}
		public string DeviceId { get; set; }
		public OsType OsType { get; set; }
		public LoginType LogType { get; set; }
		public string AccessToken { get; set; }
	}
	public class GCLoginAnsPacket : PacketAnsPacket
	{
		public GCLoginAnsPacket()
		{
			ErrorCode = Common.ErrrorCode.SUCCESS;
		}
		public long UserId { get; set; }
	}
}
