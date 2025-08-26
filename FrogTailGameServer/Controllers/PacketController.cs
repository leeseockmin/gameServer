using FrogTailGameServer.ControllerLogic;
using Microsoft.AspNetCore.Mvc;
using Share.Packet;
using System.Text.Json.Nodes;
using FrogTailGameServer.ControllerLogic;

namespace FrogTailGameServer.Controllers
{
    [ApiController]
	[Route("[controller]")]
	[Produces("application/json")]
	public partial class PacketController : ControllerBase
	{
		private readonly ILogger<PacketController> _logger;
		private readonly IServiceProvider _serviceProvider;
		public PacketController(IServiceProvider serviceProvider, ILogger<PacketController> logger)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}
	

		[HttpPost(Name = "PacketEvent")]
		public async Task<PacketAnsPacket> PacketEvent([FromBody] PacketReqeustBase packetBase)
		{
			PacketAnsPacket ans = null;
			try
			{
				var packetHandler = _serviceProvider.GetRequiredService<PacketHandler>();
				ans = await packetHandler.GetExcuteAPI(packetBase);
			}
			catch(Exception ex)
			{
				_logger.LogError(ex.Message);
			}
			return ans;
		}
	}
}
