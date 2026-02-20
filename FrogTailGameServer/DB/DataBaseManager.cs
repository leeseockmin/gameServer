using DataBase.AccountDB;
using DataBase.GameDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Transactions;

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

        private readonly IDbContextFactory<GameDBContext> _gameContextFactory;
        private readonly IDbContextFactory<AccountDBContext> _accountContextFactory;
        private readonly ILogger<DataBaseManager> _logger;

        public DataBaseManager(
            ILogger<DataBaseManager> logger,
            IDbContextFactory<AccountDBContext> accountContextFactory,
            IDbContextFactory<GameDBContext> gameContextFactory)
        {
            _logger = logger;
            _gameContextFactory = gameContextFactory;
            _accountContextFactory = accountContextFactory;
        }

        private async Task<DbContext> GetDBContext(DBtype dbtype)
        {
            return dbtype switch
            {
                DBtype.Account => await _accountContextFactory.CreateDbContextAsync(),
                DBtype.Game    => await _gameContextFactory.CreateDbContextAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(dbtype), $"Unsupported DBtype: {dbtype}")
            };
        }

        public async Task DBContextExecute(DBtype dbtype, Func<DbConnection, Task> func)
        {
            await using var context = await GetDBContext(dbtype);
            try
            {
                await context.Database.OpenConnectionAsync();
                await func.Invoke(context.Database.GetDbConnection());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DBContextExecute Error");
                throw;
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }

        /// <summary>
        /// dbtype 순서와 DB 갖고오는 값이 같아야 됩니다.
        /// </summary>
        public async Task DBContextExecute(DBtype dbtype1, DBtype dbtype2, Func<DbConnection, DbConnection, Task> func)
        {
            await using var context1 = await GetDBContext(dbtype1);
            await using var context2 = await GetDBContext(dbtype2);
            try
            {
                await context1.Database.OpenConnectionAsync();
                await context2.Database.OpenConnectionAsync();
                await func.Invoke(
                    context1.Database.GetDbConnection(),
                    context2.Database.GetDbConnection());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DBContextExecute Error");
                throw;
            }
            finally
            {
                await context1.Database.CloseConnectionAsync();
                await context2.Database.CloseConnectionAsync();
            }
        }

        public async Task DBContextExecuteTransaction(DBtype dbtype, Func<DbConnection, Task<bool>> func)
        {
            await using var context = await GetDBContext(dbtype);
            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                await context.Database.OpenConnectionAsync();
                bool isSuccess = await func.Invoke(context.Database.GetDbConnection());
                if (isSuccess)
                {
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction Error");
                throw;
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }

        public async Task DBContextExecuteTransaction(DBtype dbtype1, DBtype dbtype2, Func<DbConnection, DbConnection, Task<bool>> func)
        {
            await using var context1 = await GetDBContext(dbtype1);
            await using var context2 = await GetDBContext(dbtype2);
            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                await context1.Database.OpenConnectionAsync();
                await context2.Database.OpenConnectionAsync();

                bool isSuccess = await func.Invoke(
                    context1.Database.GetDbConnection(),
                    context2.Database.GetDbConnection());

                if (isSuccess)
                {
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction Error");
                throw;
            }
            finally
            {
                await context1.Database.CloseConnectionAsync();
                await context2.Database.CloseConnectionAsync();
            }
        }
    }
}
