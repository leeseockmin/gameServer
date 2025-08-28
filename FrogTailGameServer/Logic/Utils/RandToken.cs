using System.Security.Cryptography;

namespace FrogTailGameServer.Logic.Utils
{
	public class RandToken
	{
		public static string GenerateUniqueToken()
		{
			// 랜덤 32바이트
			var randomBytes = new byte[32];
			RandomNumberGenerator.Fill(randomBytes);
			string randomPart = Convert.ToBase64String(randomBytes);

			// 유니크 보장을 위한 요소 (예: GUID + UserId + Timestamp)
			string uniquePart = $"{Guid.NewGuid()}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

			return $"{randomPart}.{uniquePart}";
		}
	}
}
