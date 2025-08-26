using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBase.GameDB
{
    public class User
    {
        [Key]
        public long userId { get; set; }
        [Required]
        [StringLength(50)]
        public string nickName { get; set; }
    }
}
