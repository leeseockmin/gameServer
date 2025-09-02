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
						bool isSuccess = true;
						DateTime now = DateTime.UtcNow;
						if(string.IsNullOrEmpty(recvPacket.AccessToken) == false)
						{
							_logger.LogError($"Not Invailid AccessToken : {recvPacket.AccessToken}");
							ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_USER_TOKEN;
							isSuccess = false;
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
										isSuccess = false;
										break;
									}
								}
								break;
							default:
								{
									_logger.LogError($"Not Invailid LogType : {recvPacket.LogType}");
									ans.ErrorCode = Share.Common.ErrrorCode.INVAILD_USER_TOKEN;
									isSuccess = false;
								}
								break;
						}

						if (isSuccess == false)
						{
							break;
						}
						// this.GetUserSession<RedisClient.UserSession>();
						bool isCreate = false;
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
								getAccountInfo.lastLoginTime = now;

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
							}

							return true;
						});

						
						if (isCreate)
						{

						}

					
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

						ans.UserId = userSession.userId;
					}
					catch (Exception ex)
					{
						_logger.LogError($"Invaild Error : {ex.Message}");
					}

					

				} while (false);
				
				

			}
			return ans;
		}
	}
}
