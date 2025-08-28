using System.Security.Claims;
using System.Security.Principal;
using FrogTailGameServer;
using FrogTailGameServer.ControllerLogic;
using FrogTailGameServer.MiddleWare.User;

namespace FrogTailGameServer.MiddleWare.User
{
    public class CustomIdentity : IIdentity
	{
        public CustomIdentity(string userId)
        {
            UserId = userId;
            AuthenticationType = "CustomAuthentication";

		}
        public string UserId { get; private set; }
        public int Frame { get; private set; }
        public string AuthenticationType { get; set; }
        public bool IsAuthenticated { get { return true; } }
        public string Name { get; set; }
        public Common.Redis.RedisClient.UserSession UserSession { get; set; }

    }

    public class CustomPrincipal : ClaimsPrincipal
	{
		private CustomIdentity _identity;

		public CustomPrincipal(CustomIdentity identity)
		{
			_identity = identity;
		}
		public IIdentity Identity
		{
			get { return _identity; }
		}

		public bool IsInRole(string role)
		{

			return false;
		}
	}

}
