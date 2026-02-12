using Common.Redis;
using FrogTailGameServer.MiddleWare.Secret;
using FrogTailGameServer.MiddleWare.User;
using Microsoft.Extensions.Primitives;
using Serilog;
using System.Net;

namespace FrogTailGameServer.MiddleWare
{
	public class CustomMiddleWare
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<CustomMiddleWare> _logger;
		private readonly IWebHostEnvironment _env;
		private readonly bool _devMode = false;
		private readonly IServiceProvider _serviceProvider;

		private static readonly string[] AnonymousPaths = new[]
		{
			"/api/auth/login",
			"/api/auth/verify",
			"/swagger"
		};

		public CustomMiddleWare(IServiceProvider serviceProvider, RequestDelegate next, ILogger<CustomMiddleWare> logger, IWebHostEnvironment env)
		{
			_next = next;
			_logger = logger;
			_env = env;
			_devMode = _env.IsDevelopment();
			_serviceProvider = serviceProvider;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				var httpStatusCode = await IsAuth(context);
				if (httpStatusCode != HttpStatusCode.OK)
				{
					context.Response.StatusCode = (int)httpStatusCode;
					Log.Error($"Unauthorized request: {httpStatusCode}");
					return;
				}

				SendResponse(context);

				await _next(context);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Middleware authentication error");
				context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
			}
		}

		private Task<HttpStatusCode> IsAuth(HttpContext context)
		{
			var path = context.Request.Path.Value ?? string.Empty;

			foreach (var anonymousPath in AnonymousPaths)
			{
				if (path.StartsWith(anonymousPath, StringComparison.OrdinalIgnoreCase))
				{
					return Task.FromResult(HttpStatusCode.OK);
				}
			}

			return ValidateSession(context);
		}

		private async Task<HttpStatusCode> ValidateSession(HttpContext context)
		{
			var headers = context.Request.Headers;
			if (headers == null)
			{
				return HttpStatusCode.Unauthorized;
			}

			if (!headers.ContainsKey("X-UserId") || !headers.ContainsKey("Authorization"))
			{
				return HttpStatusCode.Unauthorized;
			}

			CustomIdentity identity = await GetIdentity(headers["X-UserId"], headers["Authorization"]);
			if (identity == null || identity.UserSession == null)
			{
				return HttpStatusCode.Unauthorized;
			}

			context.User = new CustomPrincipal(identity);
			return HttpStatusCode.OK;
		}

		private async Task<CustomIdentity> GetIdentity(StringValues x_userId, StringValues userToken)
		{
			CustomIdentity identity = null;
			string userId = "";
			if (_devMode == false)
			{
				userId = SecretManager.GetInstance().GetDecryptString(x_userId);
				identity = new CustomIdentity(userId);
			}
			else
			{
				userId = x_userId;
			}

			if (identity == null)
			{
				identity = new CustomIdentity(userId);
			}

			var redisClient = _serviceProvider.GetService<RedisClient>();
			if (redisClient != null)
			{
				var userSession = await redisClient.GetUserSession(userId);
				if (userSession != null)
				{
					if (userSession.userToken.CompareTo(userToken) != 0)
					{
						return null;
					}
					await redisClient.AddUserSessionExpireTime(userId);
					identity.UserSession = userSession;
				}
				else
				{
					return null;
				}
			}

			return identity;
		}

		private void SendResponse(HttpContext context)
		{
			if (!context.Response.HasStarted)
			{
				context.Response.OnStarting(state =>
				{
					var httpContext = (HttpContext)state;
					var claimsPrincipal = httpContext.User as CustomPrincipal;
					if (claimsPrincipal == null)
					{
						return Task.CompletedTask;
					}

					var identity = claimsPrincipal.Identity as CustomIdentity;
					if (identity == null || identity.UserSession == null)
					{
						return Task.CompletedTask;
					}
					var userSession = identity.UserSession;

					string encryptedUserId = "";
					if (_devMode == false)
					{
						encryptedUserId = SecretManager.GetInstance().EncryptString(userSession.userId.ToString());
					}

					if (httpContext.Response.Headers.ContainsKey("X-UserId") == false)
					{
						httpContext.Response.Headers.Add("X-UserId", $"{encryptedUserId}");
					}
					else
					{
						httpContext.Response.Headers["X-UserId"] = $"{encryptedUserId}";
					}

					if (httpContext.Response.Headers.ContainsKey("Authorization") == false)
					{
						httpContext.Response.Headers.Add("Authorization", $"Bearer {userSession.userToken}");
					}
					else
					{
						httpContext.Response.Headers["Authorization"] = $"Bearer {userSession.userToken}";
					}

					return Task.CompletedTask;
				}, context);
			}
		}
	}
}
