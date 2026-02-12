using Common.Redis;
using DataBase.AccountDB;
using DataBase.GameDB;
using DB;
using FrogTailGameServer.Logic.Utils;
using Serilog;
using Share.Common;
using Share.Packet;

namespace FrogTailGameServer.Services
{
	public class AuthService
	{
		private readonly DataBaseManager _dataBaseManager;
		private readonly RedisClient _redisClient;

		public AuthService(DataBaseManager dataBaseManager, RedisClient redisClient)
		{
			_dataBaseManager = dataBaseManager;
			_redisClient = redisClient;
		}

		public async Task<GCLoginAnsPacket> VerifyLoginAsync(CGVerityLoginReqPacket req)
		{
			var ans = new GCLoginAnsPacket();

			if (string.IsNullOrEmpty(req.AccessToken))
			{
				Log.Error("[VerifyLogin] AccessToken is empty or null");
				ans.ErrorCode = ErrrorCode.INVAILD_USER_TOKEN;
				return ans;
			}

			switch (req.LoginType)
			{
				case LoginType.Guest:
					break;
				case LoginType.Google:
				case LoginType.Apple:
				case LoginType.Email:
				{
					var loginType = await FireBase.GetLoginProviderAsync(req.AccessToken);
					if (loginType != req.LoginType)
					{
						Log.Error($"[VerifyLogin] Invalid LoginType: {req.LoginType}");
						ans.ErrorCode = ErrrorCode.INVAILD_PACKET_INFO;
						return ans;
					}
					break;
				}
				default:
					Log.Error($"[VerifyLogin] Unsupported LoginType: {req.LoginType}");
					ans.ErrorCode = ErrrorCode.INVAILD_USER_TOKEN;
					return ans;
			}

			return ans;
		}

		public async Task<GCLoginAnsPacket> LoginAsync(CGLoginReqPacket req)
		{
			var ans = new GCLoginAnsPacket();

			if (string.IsNullOrEmpty(req.AccessToken))
			{
				Log.Error("[Login] AccessToken is empty or null");
				ans.ErrorCode = ErrrorCode.INVAILD_USER_TOKEN;
				return ans;
			}

			if (string.IsNullOrEmpty(req.NickName))
			{
				Log.Error("[Login] NickName is empty or null");
				ans.ErrorCode = ErrrorCode.INVAILD_NICK_NAME;
				return ans;
			}

			switch (req.OsType)
			{
				case OsType.AOS:
				case OsType.IOS:
				case OsType.Windows:
					break;
				default:
					ans.ErrorCode = ErrrorCode.INVAILD_PACKET_INFO;
					return ans;
			}

			switch (req.LoginType)
			{
				case LoginType.Guest:
					break;
				case LoginType.Google:
				case LoginType.Apple:
				{
					var loginType = await FireBase.GetLoginProviderAsync(req.AccessToken);
					if (loginType != req.LoginType)
					{
						ans.ErrorCode = ErrrorCode.INVAILD_PACKET_INFO;
						return ans;
					}
					break;
				}
				default:
					Log.Error($"[Login] Unsupported LoginType: {req.LoginType}");
					ans.ErrorCode = ErrrorCode.INVAILD_USER_TOKEN;
					return ans;
			}

			DateTime now = DateTime.UtcNow;
			bool isCreate = false;
			long accountId = 0;

			await _dataBaseManager.DBContextExcuteTransaction(DataBaseManager.DBtype.Account, async (accountDBConnection) =>
			{
				Account getAccountInfo = null;
				var getAccountLinkInfo = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.GetAccountLinkInfo(accountDBConnection, req.LoginType, req.AccessToken);
				if (getAccountLinkInfo == null)
				{
					getAccountInfo = new Account();
					getAccountInfo.osType = req.OsType;
					getAccountInfo.deviceId = req.DeviceId;
					getAccountInfo.loginType = req.LoginType;
					getAccountInfo.updateDate = now;

					long lastAccountId = await DB.Data.Logic.AccountDBLogic.AccountInfo.InsertAccountInfo(accountDBConnection, getAccountInfo);
					if (lastAccountId <= 0)
					{
						return false;
					}

					var newAccountLink = new AccountLink();
					newAccountLink.loginType = req.LoginType;
					newAccountLink.accessToken = req.AccessToken;
					newAccountLink.createDate = now;
					newAccountLink.accountId = lastAccountId;

					int affectedCnt = await DB.Data.Logic.AccountDBLogic.AccountLinkInfo.InsertAccountLinkInfo(accountDBConnection, newAccountLink);
					if (affectedCnt <= 0)
					{
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

					getAccountInfo.osType = req.OsType;
					getAccountInfo.loginType = req.LoginType;
					getAccountInfo.updateDate = now;
					getAccountInfo.lastLoginTime = now;

					int affectedCnt = await DB.Data.Logic.AccountDBLogic.AccountInfo.UpdateLoginAccountInfo(accountDBConnection, getAccountInfo);
					if (affectedCnt <= 0)
					{
						return false;
					}
					accountId = getAccountInfo.accountId;
				}

				return true;
			});

			if (accountId <= 0)
			{
				ans.ErrorCode = ErrrorCode.UNKNOW_ERROR;
				return ans;
			}

			await _dataBaseManager.DBContextExcuteTransaction(DataBaseManager.DBtype.Game, async (gameDBConnection) =>
			{
				UserInfo userInfo = null;
				if (isCreate)
				{
					userInfo = new UserInfo();
					userInfo.nickName = req.NickName;
					userInfo.accountId = accountId;

					long lastUserId = await DB.Data.Logic.GameDBLogic.UserInfoData.InsertUserInfo(gameDBConnection, userInfo);
					if (lastUserId <= 0)
					{
						return false;
					}
					userInfo.userId = lastUserId;
				}
				else
				{
					userInfo = await DB.Data.Logic.GameDBLogic.UserInfoData.GetUserInfoByAccountId(gameDBConnection, accountId);
					if (userInfo == null)
					{
						return false;
					}
				}
				ans.UserId = userInfo.userId;

				return true;
			});

			if (ans.UserId <= 0)
			{
				ans.ErrorCode = ErrrorCode.UNKNOW_ERROR;
				return ans;
			}

			var userSession = new RedisClient.UserSession();
			userSession.userId = ans.UserId;
			userSession.userToken = RandToken.GenerateUniqueToken();
			await _redisClient.SetUserSession(userSession);

			ans.UserToken = userSession.userToken;
			ans.UserId = userSession.userId;

			return ans;
		}
	}
}
