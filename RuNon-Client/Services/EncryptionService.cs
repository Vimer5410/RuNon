using System.Security.Cryptography;

namespace RuNon_Client.Services;

public class EncryptionService
    {
        private RSA _rsa;
        
        public EncryptionService()
        {
            // Создаем RSA ключи один раз при старте приложения
            _rsa = RSA.Create(1024); 
        }
        
        public string GetPublicKey()
        {
            return Convert.ToBase64String(_rsa.ExportRSAPublicKey());
        }
        
        public byte[] DecryptAESKey(string encryptedAESKey)
        {
            return _rsa.Decrypt(Convert.FromBase64String(encryptedAESKey), RSAEncryptionPadding.OaepSHA256);
        }
        
        
        public static byte[] Encrypt(string text, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(cs))
                            {
                                sw.Write(text);
                            }
                        }
                        return ms.ToArray();
                    }
                }
            }
        }

        public static string Decrypt(byte[] text, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    using (MemoryStream ms = new MemoryStream(text))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
        
        public void Dispose()
        {
            _rsa?.Dispose();
        }
    }