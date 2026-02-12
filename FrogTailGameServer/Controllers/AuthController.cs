using FrogTailGameServer.Services;
using Microsoft.AspNetCore.Mvc;
using Share.Packet;

namespace FrogTailGameServer.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Produces("application/json")]
	public class AuthController : ControllerBase
	{
		private readonly ILogger<AuthController> _logger;
		private readonly AuthService _authService;

		public AuthController(ILogger<AuthController> logger, AuthService authService)
		{
			_logger = logger;
			_authService = authService;
		}

		[HttpPost("verify")]
		public async Task<GCLoginAnsPacket> VerifyLogin([FromBody] CGVerityLoginReqPacket req)
		{
			try
			{
				return await _authService.VerifyLoginAsync(req);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[AuthController] VerifyLogin error");
				return new GCLoginAnsPacket { ErrorCode = Share.Common.ErrrorCode.UNKNOW_ERROR };
			}
		}

		[HttpPost("login")]
		public async Task<GCLoginAnsPacket> Login([FromBody] CGLoginReqPacket req)
		{
			try
			{
				return await _authService.LoginAsync(req);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[AuthController] Login error");
				return new GCLoginAnsPacket { ErrorCode = Share.Common.ErrrorCode.UNKNOW_ERROR };
			}
		}
	}
}
