using Common.Redis;
using DataBase.GameDB;
using FrogTailGameServer.ControllerLogic;
using FrogTailGameServer.MiddleWare.Secret;
using FrogTailGameServer.MiddleWare.User;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Converters;
using Share.Packet;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using static Dapper.SqlMapper;

namespace FrogTailGameServer.MiddleWare
{
	public class CustomMiddleWare
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<CustomMiddleWare> _logger;
		private readonly IWebHostEnvironment _env;
		private readonly bool _devMode = false;
		private readonly IServiceProvider _serviceProvider;
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
					throw new Exception($"Not Unauthoize User ErrorCode:{httpStatusCode}");
				}

				if (context.User.Identity.IsAuthenticated == false)
				{
					context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
					throw new Exception($"context.User.Identity.IsAuthenticated  ErrorCode:{httpStatusCode}");
				}
				SendResponse(context);

				await _next(context);
			}
			catch(Exception ex)
			{
				_logger.LogError(ex.Message);
				context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
			}
			
		}

		private async Task<HttpStatusCode> IsAuth(HttpContext context)
		{
			string requestBody = string.Empty;

			context.Request.EnableBuffering();
			long originalPosition = context.Request.Body.Position;
			using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 2048, true))
			{
				requestBody = await reader.ReadToEndAsync();
			}
			context.Request.Body.Position = originalPosition;

			var receivePacket = Newtonsoft.Json.JsonConvert.DeserializeObject<PacketReqeustBase>(requestBody);
			if(receivePacket == null)
			{
				return HttpStatusCode.BadRequest;
			}
			if(receivePacket.RequestId == PacketId.None)
			{
				receivePacket.RequestId = PacketId.CG_Login_Req_Packet_Id;
			}

			if (receivePacket.RequestId == PacketId.CG_Login_Req_Packet_Id)
			{
				return HttpStatusCode.OK;
			}

			var headers = context.Request.Headers;
			if(headers == null || headers.Count < 2)
			{
				return HttpStatusCode.Unauthorized;
			}

			CustomIdentity idenytity = await GetIdentity(headers.ElementAt(0).Value, headers.ElementAt(1).Value);
			if(idenytity == null || idenytity.UserSession == null)
			{
				return HttpStatusCode.Unauthorized;
			}

			context.User = new CustomPrincipal(idenytity);
			return HttpStatusCode.OK;
		}

		private async Task<CustomIdentity> GetIdentity(StringValues x_userId, StringValues userToken)
		{
		
			CustomIdentity idenytity = null;
			string userId = "";
			if (_devMode == false)
			{
				userId = SecretManager.GetInstance().GetDecryptString(x_userId);
				idenytity = new CustomIdentity(userId);
			}
			else
			{
				userId = x_userId;
			}

			if (idenytity == null)
			{
				idenytity = new CustomIdentity(userId);
			}

			var redisClient = _serviceProvider.GetService<RedisClient>();
			if(redisClient != null)
			{
				var userSession = await redisClient.GetUserSession(userId);
				if(userSession != null)
				{
					if(userSession.userToken.CompareTo(userToken) != 0)
					{
						return null;
					}
					await redisClient.AddUserSessionExpireTime(userId);
					idenytity.UserSession = userSession;
				}
				else
				{
					return null;
				}
			}

			return idenytity;
		}
		private string GetAuthHeader(string authHeader)
		{
			var getParametars = authHeader.Split(":");
			if(getParametars == null || getParametars.Count() != 2)
			{
				return null;
			}

			return getParametars.LastOrDefault();
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
						return null;
					}

					var identity = claimsPrincipal.Identity as CustomIdentity;
					var userSession = identity.UserSession;

					string ecnrptUserId = "";
					if (_devMode == false)
					{
						ecnrptUserId = SecretManager.GetInstance().EncryptString(userSession.userId.ToString());
					}

					if (httpContext.Response.Headers.ContainsKey("X-UserId") == false)
					{
						httpContext.Response.Headers.Add("X-UserId", $"{ecnrptUserId}");
					}
					else
					{
						httpContext.Response.Headers["X-UserId"] = $"{ecnrptUserId}";
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
