using Common.Redis;
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

			if (receivePacket.RequestId == PacketId.CG_Login_Req_Packet_Id)
			{
				return HttpStatusCode.OK;
			}

			StringValues headers = context.Request.Headers["Authorization"];
			if(headers == string.Empty)
			{
				return HttpStatusCode.Unauthorized;
			}

			CustomIdentity idenytity = await GetIdentity(headers);
			if(idenytity == null || idenytity.UserSession == null)
			{
				return HttpStatusCode.Unauthorized;
			}

			context.User = new CustomPrincipal(idenytity);
			return HttpStatusCode.OK;
		}

		private async Task<CustomIdentity> GetIdentity(StringValues authHeader)
		{
			//var getParameter = GetAuthHeader(authHeader.ToString());
			//if(getParameter == null || getParameter.Count() != 2)
			//{
			//	return null;
			//}
			CustomIdentity idenytity = null;
			if (_devMode == false)
			{
				var userId = SecretManager.GetInstance().GetDecryptString(authHeader);
				idenytity = new CustomIdentity(userId);
			}
			if (idenytity == null)
			{
				idenytity = new CustomIdentity(authHeader);
			}

			var redisClient = _serviceProvider.GetService<RedisClient>();
			if(redisClient != null)
			{
				//[TODO]가져오고 나서 다시 한번 Session 시간 늘려준다 현재는 개발용이기 떄문에 추후 ExpireTime 추가 
				var userSession = await redisClient.GetUserSession(Convert.ToInt64(authHeader));
				idenytity.UserSession = userSession;
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
					string authHeader = identity.UserId;
					if (_devMode == false)
					{
						authHeader = SecretManager.GetInstance().EncryptString(authHeader);
					}

					if (httpContext.Response.Headers.ContainsKey("Authorization") == false)
					{
						httpContext.Response.Headers.Add("Authorization", $"{authHeader}");
					}
					else
					{
						httpContext.Response.Headers["Authorization"] = $"{authHeader}";
					}


					return Task.CompletedTask;
				}, context);
			}
			
			


		}
	}
}
