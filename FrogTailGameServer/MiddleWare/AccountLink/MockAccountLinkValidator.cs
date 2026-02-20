using Share.Common;

namespace FrogTailGameServer.MiddleWare.AccountLink
{
    public class MockAccountLinkValidator : IAccountLinkValidator
    {
        private readonly HashSet<string> _validKeys;
        private readonly ILogger<MockAccountLinkValidator> _logger;

        public MockAccountLinkValidator(IConfiguration configuration, ILogger<MockAccountLinkValidator> logger)
        {
            _logger = logger;
            var keys = configuration.GetSection("AccountLinkValidator:ValidKeys").Get<List<string>>();
            _validKeys = keys != null ? new HashSet<string>(keys) : new HashSet<string>();
        }

        public Task<bool> ValidateAsync(LoginType loginType, string accessToken)
        {
            var isValid = _validKeys.Contains(accessToken);
            if (!isValid)
            {
                _logger.LogWarning("[MockAccountLinkValidator] Invalid accessToken for LoginType: {LoginType}", loginType);
            }
            return Task.FromResult(isValid);
        }
    }
}
