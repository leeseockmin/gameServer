using Microsoft.AspNetCore.Mvc;
using Share.Packet;
using System.Text.Json.Nodes;
using FrogTailGameServer.ControllerLogic;
namespace FrogTailGameServer.Controllers
{
    [ApiController]
	[Route("[controller]")]
	[Produces("application/json")]
	public class TestController : ControllerBase
	{
		private readonly ILogger<TestController> _logger;
		private readonly IServiceProvider _serviceProvider;

		public TestController(IServiceProvider serviceProvider, ILogger<TestController> logger)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}
		/// <summary>
		/// Sagger Test 용
		/// </summary>
		/// <param name="packetId"></param>
		/// <param name="packetBase"></param>
		/// <returns></returns>

		[HttpPost(Name = "SwaggerPacket")]
		public async Task<PacketAnsPacket> SwaggerPacket(long userId, PacketId packetId, JsonObject packetBase)
		{
			PacketAnsPacket response = null;
			PacketReqeustBase receivePacket = new PacketReqeustBase(packetId);
			receivePacket.PacketBody = packetBase.ToJsonString();
			try
			{
				var packetHandler = _serviceProvider.GetRequiredService<PacketHandler>();
				response = await packetHandler.GetExcuteAPI(receivePacket);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
			}
			return response;
		}


	}
}
