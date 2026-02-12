using System.Text;
using Share.Packet;

namespace SocketServer.Network
{
	/// <summary>
	/// 패킷 헤더: [PacketSize(4byte)] [PacketId(4byte)] [Body...]
	/// </summary>
	public static class PacketHeader
	{
		public const int HeaderSize = 8; // PacketSize(4) + PacketId(4)

		/// <summary>
		/// 바이트 배열에서 패킷 크기 읽기
		/// </summary>
		public static int ReadPacketSize(ArraySegment<byte> buffer)
		{
			return BitConverter.ToInt32(buffer.Array!, buffer.Offset);
		}

		/// <summary>
		/// 바이트 배열에서 PacketId 읽기
		/// </summary>
		public static PacketId ReadPacketId(ArraySegment<byte> buffer)
		{
			return (PacketId)BitConverter.ToInt32(buffer.Array!, buffer.Offset + 4);
		}

		/// <summary>
		/// 패킷 직렬화: 헤더 + JSON body → byte[]
		/// </summary>
		public static ArraySegment<byte> Serialize(PacketReqeustBase packet)
		{
			string json = Newtonsoft.Json.JsonConvert.SerializeObject(packet);
			byte[] bodyBytes = Encoding.UTF8.GetBytes(json);
			int packetSize = HeaderSize + bodyBytes.Length;

			var segment = SendBufferHelper.Open(packetSize);
			if (segment == default)
				return default;

			// PacketSize
			Array.Copy(BitConverter.GetBytes(packetSize), 0, segment.Array!, segment.Offset, 4);
			// PacketId
			Array.Copy(BitConverter.GetBytes((int)packet.RequestId), 0, segment.Array!, segment.Offset + 4, 4);
			// Body
			Array.Copy(bodyBytes, 0, segment.Array!, segment.Offset + HeaderSize, bodyBytes.Length);

			return SendBufferHelper.Close(packetSize);
		}

		/// <summary>
		/// 패킷 역직렬화: byte[] → PacketReqeustBase
		/// </summary>
		public static PacketReqeustBase Deserialize(ArraySegment<byte> buffer)
		{
			int packetSize = ReadPacketSize(buffer);
			PacketId packetId = ReadPacketId(buffer);

			int bodyLength = packetSize - HeaderSize;
			string json = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset + HeaderSize, bodyLength);

			var packet = new PacketReqeustBase(packetId);
			packet.PacketBody = json;

			return packet;
		}
	}
}
