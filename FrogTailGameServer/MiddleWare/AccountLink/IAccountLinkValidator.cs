using Share.Common;

namespace FrogTailGameServer.MiddleWare.AccountLink
{
    public interface IAccountLinkValidator
    {
        Task<bool> ValidateAsync(LoginType loginType, string accessToken);
    }
}
