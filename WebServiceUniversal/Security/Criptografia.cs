using System.Security.Cryptography;
using System.Text;

namespace WebServiceUniversal.Security
{
    public class Criptografia
    {
        private static readonly string ChaveMestra = "MinhaChaveSecretaMuitoDificil123";// Deve ter exatamente 32 caracteres para AES-256
        private static readonly string IV = "1234567890123456";// Deve ter exatamente 16 caracteres para AES

        public static string Criptografar(string textoPuro)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(ChaveMestra);
                aes.IV = Encoding.UTF8.GetBytes(IV);

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(textoPuro);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        public static string Descriptografar(string textoCriptografado)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(ChaveMestra);
                aes.IV = Encoding.UTF8.GetBytes(IV);

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(textoCriptografado)))
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
}
