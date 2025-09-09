
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Share.Common;
using Serilog;
using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
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
		public class FirebaseClaim
		{
			public FirebaseIdentities identities { get; set; }
			public string sign_in_provider { get; set; }
		}

		public class FirebaseIdentities
		{
			
		}
		public static async Task<LoginType> GetLoginProviderAsync(string idToken)
		{
			// 1. 토큰 검증
			try
			{
				FirebaseToken decoded = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
				;
				
				if (decoded.Claims.TryGetValue("firebase", out object firebaseObj))
				{
					string jsonString = firebaseObj.ToString();
					// 4. JSON 파싱
					var firbaseClaim= JsonConvert.DeserializeObject<FirebaseClaim>(jsonString);
					switch (firbaseClaim.sign_in_provider)
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
