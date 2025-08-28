using Common.Redis;
using FrogTailGameServer.MiddleWare.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Security.Claims;

namespace FrogTailGameServer.ControllerLogic
{
	public partial class PacketHandler
	{
		//[TODO]
		//Session 저장 기준을 AccessToken값을 따로 만들 예정

		private T GetUserSession<T>() where T : class
		{
			var claimsPrincipal = _actionContextAccessor.ActionContext.HttpContext.User as CustomPrincipal;
			if (claimsPrincipal == null)
			{
				return null;
			}
			var customIdenTity = claimsPrincipal.Identity as CustomIdentity;
			if(customIdenTity == null)
			{
				return null;
			}

			return customIdenTity.UserSession as T;
		}

		void SetUserSession(RedisClient.UserSession userSession)
		{
			CustomIdentity customIdentity = new CustomIdentity(userSession.userId.ToString());
			_actionContextAccessor.ActionContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(customIdentity);
		}

	}

}
