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

		public async Task<UserSession> GetUserSession(string userId)
		{
			var getKey = string.Format(RedisKey.UserKey, userId);
			var getUserSession = await HashGet<UserSession>(getKey, RedisHahField.SessionFieldKey);
			return getUserSession;
		}
		public async Task SetUserSession(RedisClient.UserSession userSession)
		{
			var getKey = string.Format(RedisKey.UserKey, userSession.userId);
			await HashSet<UserSession>(getKey, RedisHahField.SessionFieldKey, userSession);
			await ExpiryAsync(getKey, TimeSpan.FromHours(12));
		}

		public async Task AddUserSessionExpireTime(string userId)
		{
			var getKey = string.Format(RedisKey.UserKey, userId);
			await ExpiryAsync(getKey, TimeSpan.FromHours(12));
		}

		public async Task RemoveUserSession(long userId)
		{
			var getKey = string.Format(RedisKey.UserKey, userId);
			await HashDelete(getKey, RedisHahField.SessionFieldKey);
		}

	}
}
