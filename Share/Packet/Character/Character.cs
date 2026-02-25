using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Packet
{
	public class CGCreateCharacterReqPacket : PackettBase
	{
		public CGCreateCharacterReqPacket()
		{

		}
		public int characterId { get; set; }
	}

	public class GCCreateCharacterAnsPacket : PacketAnsPacket
	{
		public GCCreateCharacterAnsPacket()
		{
			ErrorCode = Share.Common.ErrorCode.SUCCESS;
		}
	}
}
