using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.AccountDB
{
    public class AccountDBContext : DbContext
    {
		public AccountDBContext(DbContextOptions<AccountDBContext> options) : base(options)
        {
		

		}
		public DbSet<Account> account { get; set; }
        public DbSet<AccountLink> accountLink { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            var configureMethod = typeof(IModelCreateEntity).GetMethod("CreateModel");

            var entityTypes = typeof(AccountDBContext).Assembly.GetTypes()
                .Where(t => typeof(IModelCreateEntity).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in entityTypes)
            {
                var instance = Activator.CreateInstance(type) as IModelCreateEntity;
                configureMethod?.Invoke(instance, new object[] { modelBuilder });
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
