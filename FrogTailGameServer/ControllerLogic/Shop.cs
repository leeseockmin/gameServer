using Share.Packet;
using Share.Packet.ShopPacket;

namespace FrogTailGameServer.ControllerLogic
{
	public partial class PacketHandler
	{
		private async Task<PacketAnsPacket> ShopListReqPacketHanlder(PacketReqeustBase packet)
		{
			PacketAnsPacket ans = null;
			var recvPacket = Newtonsoft.Json.JsonConvert.DeserializeObject<CGShopListReqPacket>(packet.PacketBody);
			if (recvPacket != null)
			{
				ans = new PacketAnsPacket();
				do
				{

				}
				while (false);
				
			}

			return ans;
		}
	}
}
