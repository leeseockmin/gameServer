
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

namespace DB.Data.Logic.AccountDBLogic
{
	public static class AccountInfo
	{
		public static async Task<Account> GetAccountInfo(DbConnection accountConnection, long accountId)
		{
			var query = $"SELECT * FROM account WHERE accountId = {accountId} ";

			var result = await accountConnection.QueryFirstOrDefaultAsync<Account>(query);
			return result;
		}
	}
}
