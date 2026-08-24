using Sekurcom.Models;
using System.Threading.Tasks;

namespace Sekurcom.Providers
{
    /// <summary>
    /// Banka ödeme entegrasyonlarının soyutlandığı arayüz (DIP Prensipleri Gereği).
    /// </summary>
    public interface IPaymentProvider
    {
        /// <summary>
        /// Bankaya standart (Non-3D) ödeme isteği gönderir ve sonucu döner.
        /// </summary>
        Task<(bool IsSuccess, object BankResponse, int StatusCode)> ExecutePaymentAsync(PaymentRequestDto request, string orderId, string basariliDonusAdresi, string hataliDonusAdresi);
        
        /// <summary>
        /// 3D Secure işlemini başlatır ve müşteriyi bankaya/ödeme kuruluşuna yönlendirecek HTML formunu döner.
        /// (Base64 formatında veya düz HTML string olarak)
        /// </summary>
        Task<(bool IsSuccess, string HtmlContent, string ErrorMessage)> Initialize3DPaymentAsync(PaymentRequestDto request, string orderId, string basariliDonusAdresi, string hataliDonusAdresi);

        /// <summary>
        /// 3D Secure işleminden döndükten sonra ödemeyi tamamlar/provizyona çevirir.
        /// callbackData1: Iyzico için paymentId veya Ziraat için mdStatus vb.
        /// callbackData2: Iyzico için conversationData vb.
        /// </summary>
        Task<(bool IsSuccess, object BankResponse, int StatusCode)> Finalize3DPaymentAsync(string orderId, string callbackData1, string callbackData2);

        /// <summary>
        /// Gün sonu yapılmamış (henüz provizyonda olan) işlemi iptal eder.
        /// </summary>
        Task<(bool IsSuccess, string ErrorMessage)> CancelPaymentAsync(string orderId);

        /// <summary>
        /// Gün sonu yapılmış işlemi iade (Refund) eder.
        /// </summary>
        Task<(bool IsSuccess, string ErrorMessage)> RefundPaymentAsync(string orderId, decimal amount);
    }
}
