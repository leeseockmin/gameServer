using Share.Packet;
using Share.Common;
using System.Reflection;

namespace SocketServer.Handler
{
	public partial class SocketPacketHandler
	{
		private Dictionary<PacketId, Func<PacketReqeustBase, Task<PacketAnsPacket>>> _packetList;

		public SocketPacketHandler()
		{
			_packetList = new Dictionary<PacketId, Func<PacketReqeustBase, Task<PacketAnsPacket>>>();
		}

		public void InitPacketHandler()
		{
			foreach (PacketId packetId in Enum.GetValues(typeof(PacketId)))
			{
				var packetString = packetId.ToString();
				packetString = packetString.Replace("CG_", string.Empty);
				packetString = packetString.Replace("_Id", "Handler");
				packetString = packetString.Replace("_", string.Empty);

				MethodInfo methodInfo = GetType().GetMethod(packetString, BindingFlags.NonPublic | BindingFlags.Instance);
				if (methodInfo == null)
				{
					continue;
				}

				ParameterInfo[] parameters = methodInfo.GetParameters();
				if (parameters != null && parameters.Length > 0)
				{
					var delegateType = typeof(Func<,>).MakeGenericType(parameters[0].ParameterType, typeof(Task<PacketAnsPacket>));
					var methodDelegate = methodInfo.CreateDelegate(delegateType, this);
					var func = methodDelegate as Func<PacketReqeustBase, Task<PacketAnsPacket>>;
					if (func != null)
					{
						_packetList.Add(packetId, func);
					}
				}
			}
		}

		public async Task<PacketAnsPacket> ExecuteAsync(PacketReqeustBase packetBase)
		{
			if (!_packetList.TryGetValue(packetBase.RequestId, out var handler))
			{
				return new PacketAnsPacket { ErrorCode = ErrrorCode.INVAILD_PACKET_INFO };
			}

			var response = await handler(packetBase);
			return response;
		}
	}
}
