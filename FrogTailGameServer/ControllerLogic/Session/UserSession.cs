using Common.Redis;
using FrogTailGameServer.MiddleWare.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Security.Claims;

namespace FrogTailGameServer.ControllerLogic
{
	public partial class PacketHandler
	{

		private T GetUserSession<T>() where T : class
		{
			var claimsPrincipal = _actionContextAccessor.ActionContext.HttpContext.User as CustomPrincipal;
			if (claimsPrincipal == null || string.IsNullOrEmpty(claimsPrincipal.Identity.Name) == true)
			{
				return null;
			}
			var userIdentity = claimsPrincipal.Identity as CustomIdentity;
			return userIdentity.UserSession as T;
		}

		void SetUserSession(RedisClient.UserSession userSession)
		{
			CustomIdentity customIdentity = new CustomIdentity(userSession.UserId.ToString());
			_actionContextAccessor.ActionContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(customIdentity);
		}

	}

}
