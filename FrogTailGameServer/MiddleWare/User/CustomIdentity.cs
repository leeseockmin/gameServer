using System.Security.Claims;
using System.Security.Principal;

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
        public bool IsAuthenticated => true;
        public string? Name { get; set; }
        public Common.Redis.RedisClient.UserSession? UserSession { get; set; }
    }

    public class CustomPrincipal : ClaimsPrincipal
    {
        private readonly CustomIdentity _identity;

        public CustomPrincipal(CustomIdentity identity)
        {
            _identity = identity;
        }

        public new IIdentity Identity => _identity;

        public new bool IsInRole(string role) => false;
    }
}
