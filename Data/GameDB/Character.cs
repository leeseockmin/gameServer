using DataBase.AccountDB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace DB.Data.GameDB
{

	[Index(nameof(userId), Name = "IX_CHARACTER_USER_ID")]
	public class Character
	{
		[Key]
		[Required]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long characterId { get; set; }
		[Required]
		public long userId { get; set; }
		[Column("charactetType")]
		public int characterType { get; set; }
		public DateTime createTime { get; set; }
	}
}
