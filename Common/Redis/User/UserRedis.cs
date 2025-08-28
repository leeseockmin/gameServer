using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Redis
{
	public partial class RedisClient
	{
		public class UserSession
		{
			public long UserId { get; set; }
		}

		public async Task<UserSession> GetUserSession(long userId)
		{
			var getKey = string.Format(RedisKey.UserKey, userId);
			var getUserSession = await HashGet<UserSession>(getKey, RedisHahField.SessionFieldKey);
			return getUserSession;
		}
		public async Task SetUserSession(RedisClient.UserSession userSession)
		{
			var getKey = string.Format(RedisKey.UserKey, userSession.UserId);
			await HashSet<UserSession>(getKey, RedisHahField.SessionFieldKey, userSession);

		}

	}
}
