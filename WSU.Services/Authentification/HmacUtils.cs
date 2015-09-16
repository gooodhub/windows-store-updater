using System;
using System.Security.Cryptography;
using System.Text;

namespace WSU.Services.Authentification
{
    public static class HmacUtils
    {
        public static string GenerateHMAC(string key, string data)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(key)))
            {
                byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(signatureBytes);
            }
        }

        public static byte[] HashRequest(byte[] content)
        {
            if (content.Length == 0)
                return null;

            using (MD5 md5 = MD5.Create())
                return md5.ComputeHash(content);
        }
    }
}