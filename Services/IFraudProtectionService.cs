using System.Threading.Tasks;

namespace Sekurcom.Services
{
    /// <summary>
    /// IP bazlı kart deneme takibi ve Fraud (sahtekarlık) tespiti sağlayan servis arayüzü.
    /// </summary>
    public interface IFraudProtectionService
    {
        /// <summary>
        /// Belirtilen IP adresi ve kart numarası için ödeme denemesini kaydeder.
        /// Aynı IP'den 1 dakikada 5 farklı kart denenirse IP karaliste durumuna alınır.
        /// </summary>
        /// <param name="ipAddress">İstek atan kullanıcının IP adresi</param>
        /// <param name="cardNumber">Ödemede kullanılan kart numarası</param>
        /// <returns>IP adresi engellendi mi (IsBlacklisted)</returns>
        bool RecordAttempt(string ipAddress, string cardNumber);

        /// <summary>
        /// Belirtilen IP adresinin engelli (karalistede) olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="ipAddress">İstek atan kullanıcının IP adresi</param>
        /// <returns>Engelliyse true, değilse false</returns>
        bool IsIpBlacklisted(string ipAddress);
    }
}
