using FrogTailGameServer.MiddleWare.User;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FrogTailGameServer.MiddleWare.Secret
{
	public class SecretManager
	{
		private static SecretManager Instance = null;
		private readonly byte[] _key;
		public SecretManager()
		{
			_key = Encoding.UTF8.GetBytes("Hi");
		}

		public static SecretManager GetInstance()
		{
			if(Instance == null)
			{
				Instance = new SecretManager();
			}
			return Instance;
		}

		public string EncryptString(string plainText)
		{
			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = _key;
				aesAlg.GenerateIV();

				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				using (MemoryStream msEncrypt = new MemoryStream())
				{
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{
						using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
						{
							swEncrypt.Write(plainText);
						}
					}

					byte[] iv = aesAlg.IV;

					byte[] encryptedData = msEncrypt.ToArray();

					byte[] result = new byte[iv.Length + encryptedData.Length];

					Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
					Buffer.BlockCopy(encryptedData, 0, result, iv.Length, encryptedData.Length);

					return Convert.ToBase64String(result);
				}
			}
		}

		private string DecryptString(string cipherText)
		{
			byte[] cipherBytes = Convert.FromBase64String(cipherText);

			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = _key;

				byte[] iv = new byte[aesAlg.BlockSize / 8];
				byte[] cipherData = new byte[cipherBytes.Length - iv.Length];

				Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
				Buffer.BlockCopy(cipherBytes, iv.Length, cipherData, 0, cipherData.Length);

				aesAlg.IV = iv;

				ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

				using (MemoryStream msDecrypt = new MemoryStream(cipherData))
				{
					using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					{
						using (StreamReader srDecrypt = new StreamReader(csDecrypt))
						{
							return srDecrypt.ReadToEnd();
						}
					}
				}
			}
		}


		public string GetDecryptString(string cipherText)
		{
			if(String.IsNullOrEmpty(cipherText) == true)
			{
				return null;
			}
			var decryptString = DecryptString(cipherText);
			if(String.IsNullOrEmpty(decryptString) == true)
			{
				return null;
			}

			return decryptString;
		}


	}
}
