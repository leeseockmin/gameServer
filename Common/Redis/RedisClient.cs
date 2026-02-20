using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Common.Redis
{
	public partial class RedisClient
	{
		private readonly ILogger<RedisClient> _logger;
		private Lazy<ConnectionMultiplexer> _connectionMultiplexer;

		private string _connectionString;

		public RedisClient(IConfiguration configuration, ILogger<RedisClient> logger)
		{
			_logger = logger;
			_connectionString = configuration.GetConnectionString("RedisConnection");

			_connectionMultiplexer = new Lazy<ConnectionMultiplexer>(() =>
			{
				try
				{
					var connection = ConnectionMultiplexer.Connect(_connectionString);
					if (connection == null || connection.IsConnected == false)
					{
						throw new Exception("Failed to connect to Redis server.");
					}
					return connection;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex.Message);
					return null;
				}
			});
		}
		private ConnectionMultiplexer GetConnectionMultiplexer()
		{
			return _connectionMultiplexer.Value;
		}

		public bool IsConnect()
		{
			var redisClient = this.GetConnectionMultiplexer();
			return redisClient.IsConnected;
		}

		private IDatabase GetDatabase()
		{
			if (_connectionMultiplexer.Value.IsConnected == false)
			{
				return null;
			}
			var redisClient = this.GetConnectionMultiplexer();
			if (redisClient == null)
			{
				return null;
			}
			return redisClient.GetDatabase();
		}

		private async Task HashSet<T>(string redisKey, string hashKey, T hashValue) where T : class
		{
			try
			{
				await this.GetDatabase().HashSetAsync(redisKey, hashKey, Newtonsoft.Json.JsonConvert.SerializeObject(hashValue));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
			}
		}

		private async Task<T> HashGet<T>(string redisKey, string hashKey) where T : class
		{
			T getObject = null;
			try
			{
				var redisValue = await this.GetDatabase().HashGetAsync(redisKey, hashKey);
				if (redisValue.HasValue == false || redisValue.IsNull == true)
				{
					return null;
				}
				getObject = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(redisValue);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
			}

			return getObject;
		}

		private async Task<string> HashGet(string redisKey, string hashKey)
		{
			string getObject = null;
			try
			{
				var redisValue = await this.GetDatabase().HashGetAsync(redisKey, hashKey);
				if (redisValue.HasValue == false || redisValue.IsNull == true)
				{
					return null;
				}
				getObject = redisValue.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
			}

			return getObject;
		}

		private async Task HashDelete(string redisKey, string hashKey)
		{
			try
			{
				await this.GetDatabase().HashDeleteAsync(redisKey, hashKey);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
			}
		}

		private async Task ExpiryAsync(string redisKey, TimeSpan expire)
		{
			try
			{
				await this.GetDatabase().KeyExpireAsync(redisKey, expire);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[RedisClient] ExpiryAsync failed for key: {RedisKey}", redisKey);
			}
		}
	}
}
