using Microsoft.EntityFrameworkCore;
using Share.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBase.AccountDB
{
    public class AccountLink : IModelCreateEntity
	{
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }
        
        [Required]
        [StringLength(1024)]
        public string accessToken { get; set; }
        public LoginType loginType { get; set; }
        public long userId { get; set; }
        public DateTime createDate { get; set; }

		public void CreateModel(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<AccountLink>()
				.HasKey(x => x.id);
            modelBuilder.Entity<AccountLink>()
                .HasIndex(x => x.userId).HasDatabaseName("IX_ACCOUNT_LINX_USER_ID");
            modelBuilder.Entity<AccountLink>()
			   .HasIndex(x => new { x.loginType, x.accessToken }).HasDatabaseName("IX_ACCOUNT_LINK_LOGIN_ACCESS");


			modelBuilder.Entity<AccountLink>().ToTable<AccountLink>("accountlink");
		}

	}

}
