using Microsoft.EntityFrameworkCore;
using Share.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBase.AccountDB
{
    public class Account : IModelCreateEntity
    {
        [Key]
        public long userId { get; set; }
        [Required]
        [StringLength(1024)]
        public string deviceId { get; set; }
        public OsType osType { get; set; }
        public LoginType loginType { get; set; }
        public DateTime createDate { get; set; }
        public DateTime updateDate { get; set; }

        public void CreateModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
                .HasKey(x => x.userId);
            modelBuilder.Entity<Account>()
                .Property(d => d.deviceId)
                .IsRequired()
                .HasMaxLength(1024);
            modelBuilder.Entity<Account>().ToTable<Account>("account");
        }


    }
}
