using DataBase.AccountDB;
using Dapper;
using System.Data.Common;
using Share.Common;

namespace DB.Data.Logic.AccountDBLogic
{
	public class AccountLinkInfo
	{
		public static async Task<AccountLink> GetAccountLinkInfo(DbConnection accountConnection, LoginType loginType, string accessToken)
		{
			var query = "SELECT * FROM accountlink WHERE loginType = @LoginType AND accessToken = @AccessToken";

			var result = await accountConnection.QueryFirstOrDefaultAsync<AccountLink>(query, new { LoginType = (int)loginType, AccessToken = accessToken });
			return result;
		}

		public static async Task<List<AccountLink>> GetAccountLinkInfos(DbConnection accountConnection, long accountId)
		{
			var query = "SELECT * FROM accountlink WHERE accountId = @AccountId";

			var result = await accountConnection.QueryAsync<AccountLink>(query, new { AccountId = accountId });
			return result.ToList();
		}

		public static async Task<int> InsertAccountLinkInfo(DbConnection accountConnection, AccountLink accountLink)
		{
			var sql = @"
    INSERT INTO accountlink (accessToken, loginType, accountId, createDate)
    VALUES (@AccessToken, @LoginType, @AccountId, @CreateDate);";

			return await accountConnection.ExecuteAsync(sql, new
			{
				AccessToken = accountLink.accessToken,
				LoginType = accountLink.loginType,
				AccountId = accountLink.accountId,
				CreateDate = accountLink.createDate
			});
		}
	}
}
