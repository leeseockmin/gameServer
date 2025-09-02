
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBase.GameDB
{
    public class GameDBContext :DbContext
    {
		public GameDBContext(DbContextOptions<GameDBContext> options) : base(options)
        {

        }
        public DbSet<UserInfo> userInfo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            var createModelMethod = typeof(IModelCreateEntity).GetMethod("CreateModel");

            var entityTypes = typeof(GameDBContext).Assembly.GetTypes()
                .Where(t => typeof(IModelCreateEntity).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in entityTypes)
            {
                var instance = Activator.CreateInstance(type) as IModelCreateEntity;
                createModelMethod?.Invoke(instance, new object[] { modelBuilder });
            }

            base.OnModelCreating(modelBuilder);
        }

    }
}
