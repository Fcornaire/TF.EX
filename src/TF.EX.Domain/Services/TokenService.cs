using System.Security.Cryptography;
using System.Text;
using TF.EX.Domain.Ports;

namespace TF.EX.Domain.Services
{
    [Obsolete("Useless right now, might be one day for sending sensitive (ip) data directly by player if server die ", true)]

    public class TokenService : ITokenService
    {
        private const string PASSWORD = "7791c74c-8439-4909-a2d8-9f0e685080f0"; //TODO: Secret
        private readonly byte[] key;

        public TokenService()
        {
            byte[] salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            key = new Rfc2898DeriveBytes(PASSWORD, salt).GetBytes(32);
        }

        public string Generate(string ip)
        {

            string plaintext = ip;

            var encrypted = Encrypt($"{plaintext}_{new Random().Next()}");

            return encrypted;
        }

        public string GetIp(string token)
        {
            var decrypted = Decrypt(token);
            return decrypted.Split('_')[0];
        }

        private string Encrypt(string plaintext)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                        cs.Write(plaintextBytes, 0, plaintextBytes.Length);
                    }

                    byte[] encryptedBytes = ms.ToArray();
                    string encryptedText = Convert.ToBase64String(encryptedBytes);
                    return encryptedText;
                }
            }
        }

        private string Decrypt(string encryptedText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;

                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                using (MemoryStream ms = new MemoryStream(encryptedBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            string decryptedText = sr.ReadToEnd();
                            return decryptedText;
                        }
                    }
                }
            }
        }
    }
}
