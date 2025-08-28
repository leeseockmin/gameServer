using Common.Redis;
using DataBase.GameDB;
using FrogTailGameServer.ControllerLogic;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Asn1;
using Share.Packet;

namespace FrogTailGameServer.ControllerLogic
{
    public partial class PacketHandler
	{
		private async Task<PacketAnsPacket> LoginReqPacketHanlder(PacketReqeustBase packet)
		{
			
			GCLoginAnsPacket ans = null;
			var recvPacket = Newtonsoft.Json.JsonConvert.DeserializeObject<CGLoginReqPacket>(packet.PacketBody);
			if (recvPacket != null)
			{
				ans = new GCLoginAnsPacket();
				do
				{

					try
					{
						var redisClient = _serviceProvider.GetService<RedisClient>();
						if (redisClient == null)
						{
							ans.ErrorCode = Share.Common.ErrrorCode.UNKNOW_ERROR;
							break;
						}

						bool isCreate = false;
						var userSession = this.GetUserSession<RedisClient.UserSession>();
						if (userSession == null)
						{
							userSession = new RedisClient.UserSession();

							isCreate = true;
						}


						bool isSuccess = true;

						switch (recvPacket.LogType)
						{
							case Share.Common.LoginType.Guest:
								{
								}
								break;
							case Share.Common.LoginType.Google:
								{
								}
								break;
							case Share.Common.LoginType.Stove:
								{

								}
								break;
							default:
								{
									_logger.LogError($"Not Invailid LogType : {recvPacket.LogType}");
									isSuccess = false;
								}
								break;
						}

						if (isSuccess == false)
						{
							return ans;
						}


						await this._dataBaseManager.DBContextExcute(DB.DataBaseManager.DBtype.Account, async (accountDB) =>
						{
							var getAccountInfo = await DB.Data.Logic.AccountDBLogic.AccountInfo.GetAccountInfo(accountDB, 1);
							if (getAccountInfo == null)
							{
								getAccountInfo = new DataBase.AccountDB.Account();
							}


						});



						redisClient.SetUserSession(userSession);
						SetUserSession(userSession);

						ans.UserId = userSession.UserId;
					}
					catch (Exception ex)
					{
						_logger.LogError($"Invaild Error : {ex.Message}");
					}

					

				} while (false);
				
				

			}
			return ans;
		}

		/// <summary>
		/// TestLogin 에 경우 위에 LoginPacket 이랑 값을 맞춰줘야됌
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="packet"></param>
		/// <returns></returns>
		private async Task<PacketAnsPacket> TestLoginReqPacketHanlder(long userId, PacketReqeustBase packet)
		{
			GCLoginAnsPacket ans = null;
			var recvPacket = Newtonsoft.Json.JsonConvert.DeserializeObject<CGLoginReqPacket>(packet.PacketBody);
			if (recvPacket != null)
			{
				ans = new GCLoginAnsPacket();
				var userSession = new RedisClient.UserSession();
				userSession.UserId = 1;

				ans.UserId = userSession.UserId;
			}
			return ans;
		}
	}
}
