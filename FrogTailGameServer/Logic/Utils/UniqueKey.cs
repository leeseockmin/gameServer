namespace GameServer.Logic.Utils
{
	public class UniqueKey
	{
		private static DateTime _epochDate = new DateTime(2024, 06, 01);
		private static int _maxSize = 64;
		private static int _timeBitSize = 41;
		private static int _shardBitSize = 10;
		private static long _incrementCount;
		private static int _serverId;
		private static int _idBitCount;
		private static long _createIdCount;

		public static void LoadUniqueKey(int serverId)
		{
			_serverId = serverId;
			_idBitCount = _maxSize - _timeBitSize - _shardBitSize;
			_createIdCount = (long)Math.Pow(2, _idBitCount);
			_incrementCount = 1;
		}

		private static object _lockObject = new object();

		public static long GetKey()
		{
			var now = DateTime.UtcNow;
			var m = now - _epochDate;

			var calculateTicks = (long)m.TotalMilliseconds;
			var shiftTicks = calculateTicks << (_maxSize - _timeBitSize);

			var shiftServerId = shiftTicks | (long)(_serverId << _idBitCount);

			long createKey = 0;
			lock (_lockObject)
			{
				if(_incrementCount > _createIdCount)
				{
					_incrementCount = 1;
				}

				createKey = shiftServerId | (_incrementCount % _createIdCount);
				++_incrementCount;
			}

			return createKey;

		}
	}
}
