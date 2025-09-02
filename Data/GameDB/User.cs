using Microsoft.EntityFrameworkCore;
using Share.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBase.GameDB
{

	[Index(nameof(accountId) ,Name = "IX_USER_INFO_ACCOUNT_ID")]
	public class UserInfo
    {
		[Key]
		[Required]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long userId { get; set; }
        [Required]
        [StringLength(50)]
        public string nickName { get; set; }
		[Required]
		public long accountId { get; set; }
    }
}
