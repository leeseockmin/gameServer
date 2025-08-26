using Share.Common;
namespace Share.Packet
{
	public class PacketReqeustBase
	{
		public PacketReqeustBase()
		{
			RequestId = PacketId.None;
			PacketBody = String.Empty;
		}
		public PacketReqeustBase(PacketId packetId)
		{
			RequestId = packetId;
		}
		public PacketId RequestId;
		public string PacketBody;
	}
	public class PacketAnsPacket : PacketReqeustBase
	{
		public PacketAnsPacket() 
		{
			
		}
		public ErrrorCode ErrorCode;
	}
}