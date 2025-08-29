using Common.Redis;
using FrogTailGameServer.ControllerLogic;
using FrogTailGameServer.MiddleWare.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Share.Packet;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using DataBase;
using StackExchange.Redis.KeyspaceIsolation;
using DB;

namespace FrogTailGameServer.ControllerLogic
{
    public partial class PacketHandler
    {
        private Dictionary<PacketId, Func<PacketReqeustBase, Task<PacketAnsPacket>>> _packetList = null;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ILogger<PacketHandler> _logger;
		private readonly IServiceProvider _serviceProvider;
        private readonly DataBaseManager _dataBaseManager;
		public PacketHandler(IServiceProvider serviceProvider, IActionContextAccessor actionContextAccessor, ILogger<PacketHandler> logger, DataBaseManager dataBaseManager)
        {
            _actionContextAccessor = actionContextAccessor;
            _logger = logger;
			_packetList = new Dictionary<PacketId, Func<PacketReqeustBase, Task<PacketAnsPacket>>>();
            _serviceProvider = serviceProvider;
            _dataBaseManager = dataBaseManager;

		}

        public void InitPacketHandler()
        {
            foreach (PacketId packetId in Enum.GetValues(typeof(PacketId)))
            {
                var packetString = packetId.ToString();
                packetString = packetString.Replace("CG_", string.Empty);
                packetString = packetString.Replace("_Id", "Hanlder");
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

        public async Task<PacketAnsPacket> GetExcuteAPI(PacketReqeustBase packetBase)
        {
			if (packetBase.RequestId == PacketId.None)
			{
				packetBase.RequestId = PacketId.CG_Login_Req_Packet_Id;
			}
			var response = await _packetList[packetBase.RequestId](packetBase);
            return response;
        }


	}
}
