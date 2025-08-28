using Microsoft.EntityFrameworkCore;
using Share.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBase.AccountDB
{

	[Index(nameof(accountId), Name = "IX_ACCOUNT_LINX_ACCOUNT_ID")] 
	[Index(nameof(loginType), nameof(accessToken), Name = "IX_ACCOUNT_LINK_LOGIN_ACCESS")]
	[Table("accountlink")] // 테이블 이름 지정
	public class AccountLink : IModelCreateEntity
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long id { get; set; }

		[Required]
		[StringLength(1024)]
		public string accessToken { get; set; }

		public LoginType loginType { get; set; }
		[Required]
		public long accountId { get; set; }

		public DateTime createDate { get; set; }

		// DataAnnotation 사용하면 Fluent API 필요 없음
		public void CreateModel(ModelBuilder modelBuilder)
		{
			// 이제 비워두거나 삭제 가능
		}
	}

}
