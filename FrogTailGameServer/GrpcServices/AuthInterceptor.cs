using Common.Redis;
using FrogTailGameServer.MiddleWare.Secret;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace FrogTailGameServer.GrpcServices
{
	public class AuthInterceptor : Interceptor
	{
		private readonly RedisClient _redisClient;
		private readonly SecretManager _secretManager;
		private readonly IWebHostEnvironment _env;
		private readonly ILogger<AuthInterceptor> _logger;

		private static readonly HashSet<string> AnonymousMethods = new()
		{
			"/login.LoginService/Login",
			"/login.LoginService/VerityLogin"
		};

		public AuthInterceptor(
			RedisClient redisClient,
			SecretManager secretManager,
			IWebHostEnvironment env,
			ILogger<AuthInterceptor> logger)
		{
			_redisClient = redisClient;
			_secretManager = secretManager;
			_env = env;
			_logger = logger;
		}

		public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
			TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
		{
			if (!AnonymousMethods.Contains(context.Method))
			{
				await ValidateSession(context);
			}

			return await continuation(request, context);
		}

		private async Task ValidateSession(ServerCallContext context)
		{
			var userIdEntry = context.RequestHeaders.FirstOrDefault(e => e.Key == "x-userid")?.Value;
			var authEntry = context.RequestHeaders.FirstOrDefault(e => e.Key == "authorization")?.Value;

			if (string.IsNullOrEmpty(userIdEntry) || string.IsNullOrEmpty(authEntry))
			{
				_logger.LogWarning("[AuthInterceptor] Missing x-userid or authorization header. Method: {Method}", context.Method);
				throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing credentials"));
			}

			string userId;
			if (_env.IsDevelopment())
			{
				userId = userIdEntry;
			}
			else
			{
				string? decrypted = _secretManager.GetDecryptString(userIdEntry);
				if (string.IsNullOrEmpty(decrypted))
				{
					throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid user id"));
				}
				userId = decrypted;
			}

			var token = authEntry.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
				? authEntry.Substring(7)
				: authEntry;

			var userSession = await _redisClient.GetUserSession(userId);
			if (userSession == null || userSession.userToken != token)
			{
				_logger.LogWarning("[AuthInterceptor] Invalid session for userId: {UserId}", userId);
				throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid session"));
			}

			await _redisClient.AddUserSessionExpireTime(userId);
		}
	}
}
