using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Share.Common;
using Serilog;
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
            public FirebaseIdentities? identities { get; set; }
            public string? sign_in_provider { get; set; }
        }

        public class FirebaseIdentities
        {
        }

        public static async Task<LoginType> GetLoginProviderAsync(string idToken)
        {
            try
            {
                FirebaseToken decoded = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                if (!decoded.Claims.TryGetValue("firebase", out object? firebaseObj))
                {
                    return LoginType.None;
                }

                var jsonString = firebaseObj?.ToString();
                if (string.IsNullOrEmpty(jsonString))
                {
                    return LoginType.None;
                }

                var firebaseClaim = JsonConvert.DeserializeObject<FirebaseClaim>(jsonString);
                if (firebaseClaim == null || string.IsNullOrEmpty(firebaseClaim.sign_in_provider))
                {
                    return LoginType.None;
                }

                return firebaseClaim.sign_in_provider switch
                {
                    "google.com" => LoginType.Google,
                    "apple.com"  => LoginType.Apple,
                    "password"   => LoginType.Email,
                    _            => LoginType.None
                };
            }
            catch (FirebaseAuthException ex)
            {
                Log.Error("GetLoginProviderAsync Invalid Verify Error : {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error("GetLoginProviderAsync: {Message}", ex.Message);
            }

            return LoginType.None;
        }
    }
}
