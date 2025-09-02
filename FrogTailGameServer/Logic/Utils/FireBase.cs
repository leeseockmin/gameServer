
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Share.Common;
using Serilog;
namespace FrogTailGameServer.Logic.Utils
{
	public class FireBase
	{
		public static void InitFireBase(string json)
		{
			FirebaseApp.Create(new AppOptions
			{
				Credential = GoogleCredential.FromJson(json)
			});

		}
		public static async Task<LoginType> GetLoginProviderAsync(string idToken)
		{
			// 1. 토큰 검증
			try
			{
				FirebaseToken decoded = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

				// 2. Firebase가 제공하는 claim 안에 sign_in_provider 확인
				if (decoded.Claims.TryGetValue("firebase", out object firebaseObj))
				{
					var firebaseDict = firebaseObj as IDictionary<string, object>;
					if (firebaseDict != null)
					{
						if (firebaseDict.TryGetValue("sign_in_provider", out object providerObj))
						{
							string provider = providerObj.ToString();

							switch (provider)
							{
								case "google.com":
									return LoginType.Google;
								case "apple.com":
									return LoginType.Apple;
								case "password":
									return LoginType.Email;
							}
						}
					}
				}
			}
			catch (FirebaseAuthException ex)
			{
				Log.Error("GetLoginProviderAsync Invaild Vertify Error : " + ex.Message);
			}
			catch (Exception ex)
			{
				Log.Error("GetLoginProviderAsync: " + ex.Message);
			}
		
			return LoginType.None;
		}

	}
}
