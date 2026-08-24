using System.Security.Cryptography;
using System.Text;

namespace Sekurcom.Helpers
{
    public static class PaymentHelper
    {
        public static string CreateZiraatHash(string clientId, string orderId, string amount, string okUrl, string failUrl, string hashKey)
        {
            
            string hashString = $"{clientId}{orderId}{amount}{okUrl}{failUrl}{hashKey}";
            byte[] hashBytes = Encoding.UTF8.GetBytes(hashString);

            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] computedHash = sha512.ComputeHash(hashBytes);
                return Convert.ToBase64String(computedHash);
            }
        }
    }
}