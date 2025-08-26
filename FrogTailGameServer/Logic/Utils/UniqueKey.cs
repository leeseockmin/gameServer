namespace GameServer.Logic.Utils
{
	public class UniqueKey
	{
		private static DateTime EpochDate = new DateTime(2024, 06, 01);
		private static int MaxSize = 64;
		private static int TimeBitSize = 41;
		private static int ShardBitSize = 10;
		private static long IncreseMentCount;
		private static int ServerId;
		private static int IdBitCount;
		private static long CreateIdCount;

		public static void LoadUniqueKey(int serverId)
		{
			ServerId = serverId;
			IdBitCount = MaxSize - TimeBitSize - ShardBitSize;
			CreateIdCount = (long)Math.Pow(2, IdBitCount);
			IncreseMentCount = 1;
		}

		private static object lockObject= new object();

		public static long GetKey()
		{
			var now = DateTime.UtcNow;
			var m = now - EpochDate;
			
			var calculateTicks = (long)m.TotalMilliseconds;
			var shiftTicks = calculateTicks << (MaxSize - TimeBitSize);

			var shiftServerId = shiftTicks | (long)(ServerId << IdBitCount);

			long createKey = 0;
			lock (lockObject)
			{
				if(IncreseMentCount > CreateIdCount)
				{
					IncreseMentCount = 1;
				}

				createKey = shiftServerId | (IncreseMentCount % CreateIdCount);
				++IncreseMentCount;
			}

			return createKey;

		}
	}	
}
