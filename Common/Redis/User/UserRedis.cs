using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Redis
{
	public partial class RedisClient
	{
		public class UserSession
		{
			public long userId { get; set; }
			public string userToken { get; set; }
		}

		public async Task<UserSession> GetUserSession(string key)
		{
			var getKey = string.Format(RedisKey.UserKey, key);
			var getUserSession = await HashGet<UserSession>(getKey, RedisHahField.SessionFieldKey);
			return getUserSession;
		}
		public async Task SetUserSession(RedisClient.UserSession userSession)
		{
			var getKey = string.Format(RedisKey.UserKey, userSession.userId);
			await HashSet<UserSession>(getKey, RedisHahField.SessionFieldKey, userSession);
			await ExpiryAsync(getKey, TimeSpan.FromHours(12));
		}

		public async Task AddSessionExpireTime(string key)
		{
			var getKey = string.Format(RedisKey.UserKey, key);
			await ExpiryAsync(getKey, TimeSpan.FromHours(12));
		}

	}
}
