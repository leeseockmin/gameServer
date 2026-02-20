using DataBase.GameDB;
using Dapper;
using System.Data.Common;

namespace DB.Data.Logic.GameDBLogic
{
	public class UserInfoData
	{
		public static async Task<UserInfo> GetUserInfoByAccountId(DbConnection gameConnection, long accountId)
		{
			var query = @"
SELECT userId, nickName, accountId
FROM userInfo
WHERE accountId = @AccountId";

			var result = await gameConnection.QueryFirstOrDefaultAsync<UserInfo>(query, new { AccountId = accountId });
			return result;
		}

		public static async Task<UserInfo?> GetUserInfoByUserId(DbConnection gameConnection, long userId)
		{
			var query = @"
SELECT userId, nickName, accountId
FROM userInfo
WHERE userId = @UserId";

			var result = await gameConnection.QueryFirstOrDefaultAsync<UserInfo>(query, new { UserId = userId }).ConfigureAwait(false);
			return result;
		}

		public static async Task<long> InsertUserInfo(DbConnection gameConnection, UserInfo userInfo)
		{
			var sql = @"
    INSERT INTO userInfo(nickName, accountId)
    VALUES (@NickName, @AccountId);
    SELECT LAST_INSERT_ID();";

			var lastId = await gameConnection.QuerySingleAsync<long>(sql, new
			{
				NickName = userInfo.nickName,
				AccountId = userInfo.accountId
			});
			return lastId;
		}
	}
}
