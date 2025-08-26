using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Common.Redis
{
	public class RedisKey
	{
		public static string UserKey = "userSession:{0}";
		
	}
	public class RedisHahField
	{
		public readonly static string SessionFieldKey = "UserInfo";
	}

}
