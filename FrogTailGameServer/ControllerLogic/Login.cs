using Common.Redis;
using DataBase.AccountDB;
using DataBase.GameDB;
using FrogTailGameServer.ControllerLogic;
using FrogTailGameServer.Logic.Utils;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Asn1;
using Share.Packet;
using System.Drawing;
using System.Security.Cryptography;
using Serilog;
namespace FrogTailGameServer.ControllerLogic
{
    public partial class PacketHandler
	{

		private async Task<PacketAnsPacket> VertifyLoginReqPacketHanlder(PacketReqeustBase packet)
		{
			GCLoginAnsPacket ans = null;
			var recvPacket = Newtonsoft.Json.JsonConvert.DeserializeObject<CGLoginReqPacket>(packet.PacketBody);
			if (recvPacket == null)
			{
				ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_PACKET_INFO;
				return ans;
			}
			ans = new GCLoginAnsPacket();
			do
			{
				try
				{

					DateTime now = DateTime.UtcNow;
					if (string.IsNullOrEmpty(recvPacket.AccessToken) == false)
					{
						Log.Error($"[VertifyLogin] Not Invailid AccessToken : {recvPacket.AccessToken}");
						ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_USER_TOKEN;
						break;
					}

					if (string.IsNullOrEmpty(recvPacket.NickName) == false)
					{
						Log.Error($"[VertifyLogin] Not Invailid Nick Name: {recvPacket.NickName}");
						ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_NICK_NAME;
						break;
					}

					switch (recvPacket.LogType)
					{
						case Share.Common.LoginType.Guest:
							{
							}
							break;
						case Share.Common.LoginType.Google:
						case Share.Common.LoginType.Apple:
							{
								var loginType = await FireBase.GetLoginProviderAsync(recvPacket.AccessToken);
								if (loginType != recvPacket.LogType)
								{
									// 에러 추가
									Log.Error($"[VertifyLogin] Not Invailid LoginType: {recvPacket.LogType} , AccessToken : {recvPacket.AccessToken}");
									ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_PACKET_INFO;
									break;
								}
							}
							break;
						default:
							{
								Log.Error($"[VertifyLogin] Not Invailid LogType : {recvPacket.LogType}");
								ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_USER_TOKEN;
							}
							break;
					}

					if (ans.ErrorCode != Share.Common.ErrrorCode.SUCCESS)
					{
						break;
					}
				}
				catch (Exception ex)
				{
					Log.Error($"[VertifyLogin] Invaild Error : {ex.Message}");
				}
			}
			while (false);

			return ans;

		}

		private async Task<PacketAnsPacket> LoginReqPacketHanlder(PacketReqeustBase packet)
		{
			
			GCLoginAnsPacket ans = new GCLoginAnsPacket();
			var recvPacket = Newtonsoft.Json.JsonConvert.DeserializeObject<CGLoginReqPacket>(packet.PacketBody);
			if(recvPacket == null)
			{
				ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_PACKET_INFO;
				return ans;
			}
				
			do
			{

				try
				{
					DateTime now = DateTime.UtcNow;
					if(string.IsNullOrEmpty(recvPacket.AccessToken) == false)
					{
						Log.Error($"[LoginReqPacketHanlder] Not Invailid AccessToken : {recvPacket.AccessToken}");
						ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_USER_TOKEN;
						break;
					}

					if(string.IsNullOrEmpty(recvPacket.NickName) == false)
					{
						Log.Error($"[LoginReqPacketHanlder] Not Invailid Nick Name: {recvPacket.NickName}");
						ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_NICK_NAME;
						break;
					}

					switch(recvPacket.OsType)
					{
						case Share.Common.OsType.AOS:
						case Share.Common.OsType.IOS:
						case Share.Common.OsType.Windows:
							{
								break;
							}
						default:
							{
								ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_PACKET_INFO;
								break;
							}


					}
					if(ans.ErrorCode != Share.Common.ErrrorCode.SUCCESS)
					{
						break;
					}
						
					switch (recvPacket.LogType)
					{
						case Share.Common.LoginType.Guest:
							{
							}
							break;
						case Share.Common.LoginType.Google:
						case Share.Common.LoginType.Apple:
							{
								var loginType = await FireBase.GetLoginProviderAsync(recvPacket.AccessToken);
								if(loginType != recvPacket.LogType)
								{
									// 에러 추가
									ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_PACKET_INFO;
									break;
								}
							}
							break;
						default:
							{
								Log.Error($"[LoginReqPacketHanlder] Not Invailid LogType : {recvPacket.LogType}");
								ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_USER_TOKEN;
							}
							break;
					}

					if (ans.ErrorCode != Share.Common.ErrrorCode.SUCCESS)
					{
						break;
					}
					// this.GetUserSession<RedisClient.UserSession>();
					bool isCreate = false;
					long accountId = 0;
					await this._dataBaseManager.DBContextExcuteTransaction(DB.DataBaseManager.DBtype.Account, async (accountDBConnection) =>
					{
						Account getAccountInfo = null;
						var getAccountLinkInfo = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.GetAccountLinkInfo(accountDBConnection, recvPacket.LogType, recvPacket.AccessToken);
						if(getAccountLinkInfo == null)
						{
							getAccountInfo = new DataBase.AccountDB.Account();
							getAccountInfo.osType = recvPacket.OsType;
							getAccountInfo.deviceId = recvPacket.DeviceId;
							getAccountInfo.loginType = recvPacket.LogType;
							getAccountInfo.updateDate = now;

							long lastAccountId = await DB.Data.Logic.AccountDBLogic.AccountInfo.InsertAccountInfo(accountDBConnection, getAccountInfo);
							if(lastAccountId <= 0)
							{
								//에러 추가
								return false;
							}

							getAccountLinkInfo = new AccountLink();
							getAccountLinkInfo.loginType = recvPacket.LogType;
							getAccountLinkInfo.accessToken = recvPacket.AccessToken;
							getAccountLinkInfo.createDate = now;
							getAccountLinkInfo.accountId = lastAccountId;

							int affectd_cnt = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.InsertAccountLinkInfo(accountDBConnection, getAccountLinkInfo);
							if(affectd_cnt <= 0)
							{
								//에러 추가
								return false;
							}
							accountId = lastAccountId;

							isCreate = true;
						}
						else
						{
							getAccountInfo = await DB.Data.Logic.AccountDBLogic.AccountInfo.GetAccountInfo(accountDBConnection, getAccountLinkInfo.accountId);
							if (getAccountInfo == null)
							{
								return false;
							}

							getAccountInfo.osType = recvPacket.OsType;
							getAccountInfo.loginType = recvPacket.LogType;
							getAccountInfo.updateDate = now;
							getAccountInfo.lastLoginTime = now;

							int affectd_cnt = await DB.Data.Logic.AccountDBLogic.AccountInfo.UpdateLoginAccountInfo(accountDBConnection, getAccountInfo);
							if (affectd_cnt <= 0)
							{
								return false;
							}
							accountId = getAccountInfo.accountId;
						}

						return true;
					});

					if(accountId  <= 0)
					{
						break;
					}

					await this._dataBaseManager.DBContextExcuteTransaction(DB.DataBaseManager.DBtype.Game, async (gameDBConnection) =>
					{

						UserInfo userInfo = null;
						if (isCreate)
						{
							userInfo = new UserInfo();
							userInfo.nickName = recvPacket.NickName;
							userInfo.accountId = accountId;
						}
						else
						{

						}
						ans.UserId = userInfo.userId;

						return true;
					});


					
					var redisClient = _serviceProvider.GetService<RedisClient>();
					if (redisClient == null)
					{
						//에러 추가
						ans.ErrorCode = Share.Common.ErrrorCode.UNKNOW_ERROR;
						break;
					}

					var userSession = new RedisClient.UserSession();
					userSession.userId = ans.UserId;
					userSession.userToken = RandToken.GenerateUniqueToken();
					await redisClient.SetUserSession(userSession);
					this.SetUserSession(userSession);

					ans.UserToken = userSession.userToken;
					ans.UserId = userSession.userId;
				}
				catch (Exception ex)
				{
					Log.Error($"[LoginReqPacketHanlder] Invaild Error : {ex.Message}");
				}

			} while (false);
				
				

			return ans;
		}
	}
}
