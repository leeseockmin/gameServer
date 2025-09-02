
using DataBase;
using DataBase.AccountDB;
using DataBase.GameDB;
using Microsoft.EntityFrameworkCore;
using Dapper;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using Share.Common;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DB.Data.Logic.AccountDBLogic
{
	public class AccountLinkInfo
	{
		public static async Task<AccountLink> GetAccountLinkInfo(DbConnection accountConnection, LoginType loginType, string accessToken)
		{
			var query = $"SELECT * FROM accountLink WHERE loginType = {loginType} AND accessToken = {accessToken} ";

			var result = await accountConnection.QueryFirstOrDefaultAsync<AccountLink>(query);
			return result;
		}

		public static async Task<List<AccountLink>> GetAccountLinkInfos(DbConnection accountConnection, long accountId)
		{
			var query = $"SELECT * FROM account WHERE accountId = {accountId} ";

			var result = await accountConnection.QueryAsync<AccountLink>(query);
			return result.ToList();
		}

		public static async Task<int> InsertAccountLinkInfo(DbConnection accountConnection, AccountLink accountLink)
		{
			var sql = @"
    INSERT INTO account_link (accessToken, loginType, accountId, createDate)
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
