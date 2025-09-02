
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DB.Data.Logic.AccountDBLogic
{
	public class AccountInfo
	{
		public static async Task<Account> GetAccountInfo(DbConnection accountConnection, long accountId)
		{
			var query = $"SELECT * FROM account WHERE accountId = {accountId} ";

			var result = await accountConnection.QueryFirstOrDefaultAsync<Account>(query);
			return result;
		}
		/// <summary>
		/// 업데이트 나 Insert 구문은 인젝션 제거하기 위해서 
		/// </summary>
		/// <param name="accountConnection"></param>
		/// <param name="accountInfo"></param>
		/// <returns></returns>
		public static async Task<long> InsertAccountInfo(DbConnection accountConnection, Account accountInfo)
		{
			var sql = @"
    INSERT INTO account(deviceId, osType, loginType, createDate, updateDate, lastLoginTime)
    VALUES (@DeviceId, @OsType, @LoginType, @CreateDate, @UpdateDate, @LastLoginTime);
    SELECT LAST_INSERT_ID();";

			var lastId = await accountConnection.QuerySingleAsync<long>(sql, new
			{
				DeviceId = accountInfo.deviceId,
				OsType = accountInfo.osType,
				LoginType = accountInfo.loginType,
				CreateDate = accountInfo.createDate,
				UpdateDate = accountInfo.updateDate,
				LastLoginTime = accountInfo.lastLoginTime
			});
			return lastId;
		}

		public static async Task<int> UpdateLoginAccountInfo(DbConnection accountConnection, Account accountInfo)
		{
			var sql = @"
			UPDATE account
			SET 
				deviceId = @DeviceId,
				osType = @OsType,
				loginType = @LoginType,
				createDate = @CreateDate,
				updateDate = @UpdateDate,
				lastLoginTime = @LastLoginTime
			WHERE accountId = @Id;";

			// 실행
			return await accountConnection.ExecuteAsync(sql, new
			{
				Id = accountInfo.accountId,
				DeviceId = accountInfo.deviceId,
				OsType = accountInfo.osType,
				LoginType = accountInfo.loginType,
				CreateDate = accountInfo.createDate,
				UpdateDate = accountInfo.updateDate,
				LastLoginTime = accountInfo.lastLoginTime
			});
		}
	}
}
