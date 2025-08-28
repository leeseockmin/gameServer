
using DataBase.AccountDB;
using DataBase.GameDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace DB
{
    public class DataBaseManager
    {
        public enum DBtype
        {
            NONE = 0,
			Account = 1,
            Game = 2,
        }

        private IDbContextFactory<GameDBContext> _gameContextFactory;
        private IDbContextFactory<AccountDBContext> _acountContextFactory;
        ILogger<DataBaseManager> _logger;
        public DataBaseManager(ILogger<DataBaseManager> logger, IDbContextFactory<AccountDBContext> acountContextFactory, IDbContextFactory<GameDBContext> gameContextFactory)
        {
            _logger = logger;
            _gameContextFactory = gameContextFactory;
            _acountContextFactory = acountContextFactory;
        }

        private async Task<DbContext> GetDBContext(DBtype dbtype)
        {
            DbContext context = null;
            switch (dbtype)
            {
                case DBtype.Account:
                    {
                        context = await _acountContextFactory.CreateDbContextAsync();
                    }
                    break;
                case DBtype.Game:
                    {
                        context = await _gameContextFactory.CreateDbContextAsync();
                    }
                    break;
            }
            return context;
        }


        public async Task DBContextExcute(DBtype dbtype, Func<DbConnection, Task> func)
        {
            using (var context = await GetDBContext(dbtype))
            {
                try
                {
                    await context.Database.OpenConnectionAsync();
                    await func.Invoke(context.Database.GetDbConnection());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }

            }
        }
        /// <summary>
        /// dyType 순서와 DB 갖고오는 값이 같아야됌.
        /// </summary>
        /// <param name="dbtype1"></param>
        /// <param name="dbtype2"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public async Task DBContextExcute(DBtype dbtype1, DBtype dbtype2, Func<DbConnection, DbConnection, Task> func)
        {
            using (var context1 = await GetDBContext(dbtype1))
            using (var context2 = await GetDBContext(dbtype2))
            {
                try
                {
                    await context1.Database.OpenConnectionAsync();
                    await context2.Database.OpenConnectionAsync();
                    await func.Invoke(context1.Database.GetDbConnection(), context2.Database.GetDbConnection());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                finally
                {
                    await context1.Database.CloseConnectionAsync();
                    await context2.Database.CloseConnectionAsync();
                }

            }
        }


    }
}
