using System.Security.Cryptography;
using System.Text;

namespace FrogTailGameServer.MiddleWare.Secret
{
    /// <summary>
    /// AES-256 암호화를 담당하는 매니저 클래스.
    /// DI 컨테이너에 Singleton으로 등록하여 사용합니다.
    /// appsettings.json의 Security:EncryptionKey 값이 반드시 설정되어야 합니다.
    /// </summary>
    public class SecretManager
    {
        private readonly byte[] _key;

        private const int RequiredKeyLength = 32; // AES-256

        public SecretManager(IConfiguration configuration)
        {
            var encryptionKey = configuration.GetSection("Security")["EncryptionKey"];
            if (string.IsNullOrEmpty(encryptionKey))
            {
                throw new InvalidOperationException("Security:EncryptionKey is not configured. Please set it in appsettings.json or environment variables.");
            }

            var keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
            if (keyBytes.Length < RequiredKeyLength)
            {
                // SHA-256으로 해싱하여 정확히 32바이트 키 생성
                using var sha256 = SHA256.Create();
                _key = sha256.ComputeHash(keyBytes);
            }
            else
            {
                _key = keyBytes[..RequiredKeyLength];
            }
        }

        public string EncryptString(string plainText)
        {
            using var aesAlg = Aes.Create();
            aesAlg.Key = _key;
            aesAlg.GenerateIV();

            var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            byte[] iv = aesAlg.IV;
            byte[] encryptedData = msEncrypt.ToArray();
            byte[] result = new byte[iv.Length + encryptedData.Length];

            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedData, 0, result, iv.Length, encryptedData.Length);

            return Convert.ToBase64String(result);
        }

        private string DecryptString(string cipherText)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using var aesAlg = Aes.Create();
            aesAlg.Key = _key;

            byte[] iv = new byte[aesAlg.BlockSize / 8];
            byte[] cipherData = new byte[cipherBytes.Length - iv.Length];

            Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, iv.Length, cipherData, 0, cipherData.Length);

            aesAlg.IV = iv;

            var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using var msDecrypt = new MemoryStream(cipherData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }

        public string? GetDecryptString(string? cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return null;
            }

            var decryptString = DecryptString(cipherText);
            if (string.IsNullOrEmpty(decryptString))
            {
                return null;
            }

            return decryptString;
        }
    }
}
