using Share.Common;
namespace Share.Packet
{
	public class PacketRequestBase
	{
		public PacketRequestBase()
		{
			RequestId = PacketId.None;
			PacketBody = String.Empty;
		}
		public PacketRequestBase(PacketId packetId)
		{
			RequestId = packetId;
		}
		public PacketId RequestId;
		public string PacketBody;
	}
	public class PacketAnsPacket : PacketRequestBase
	{
		public PacketAnsPacket()
		{

		}
		public ErrorCode ErrorCode;
	}
}
