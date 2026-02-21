using Share.Common;

namespace Share.Packet
{
	public class PacketRequestBase
	{
		public PacketRequestBase()
		{
			RequestId = PacketId.None;
		}
		public PacketRequestBase(PacketId packetId)
		{
			RequestId = packetId;
		}
		public PacketId RequestId;
	}
	public class PacketAnsPacket : PacketRequestBase
	{
		public PacketAnsPacket()
		{

		}
		public ErrorCode ErrorCode;
	}
}
