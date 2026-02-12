using FrogTailGameServer.Services;
using Microsoft.AspNetCore.Mvc;
using Share.Packet.ShopPacket;

namespace FrogTailGameServer.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Produces("application/json")]
	public class ShopController : ControllerBase
	{
		private readonly ILogger<ShopController> _logger;
		private readonly ShopService _shopService;

		public ShopController(ILogger<ShopController> logger, ShopService shopService)
		{
			_logger = logger;
			_shopService = shopService;
		}

		[HttpPost("getShopList")]
		public async Task<GCShopListAnsPacket> GetShopList()
		{
			try
			{
				return await _shopService.GetShopListAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[ShopController] GetShopList error");
				return new GCShopListAnsPacket { ErrorCode = Share.Common.ErrrorCode.UNKNOW_ERROR };
			}
		}
	}
}
