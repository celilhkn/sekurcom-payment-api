using Sekurcom.Models;

namespace Sekurcom.Services
{
    /// <summary>
    /// Ödeme işlemlerinin yönetildiği ana servis arayüzü.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Doğrudan ödeme (Non-3D) işlemini gerçekleştirir.
        /// </summary>
        Task<(bool IsSuccess, string OrderId, object Body, int StatusCode)> ExecutePaymentAsync(PaymentRequestDto request, string? userId);
        
        /// <summary>
        /// Belirtilen ID'ye sahip ödeme kaydını getirir.
        /// </summary>
        Task<PaymentRecord?> GetPaymentByIdAsync(string id);
        
        /// <summary>
        /// Tüm ödeme kayıtlarını listeler.
        /// </summary>
        Task<List<PaymentRecord>> GetAllPaymentsAsync();
        
        /// <summary>
        /// 3D Secure yönlendirme HTML formunu oluşturur ve ödeme isteğini veritabanına "Pending3D" olarak kaydeder.
        /// </summary>
        Task<(bool IsSuccess, string HtmlContent, string ErrorMessage)> Initialize3DPaymentAsync(PaymentRequestDto request, string? userId);
        
        /// <summary>
        /// Bankanın veya Iyzico'nun 3D Secure işleminden dönen sonucu işler ve onaylar.
        /// </summary>
        Task<(bool IsSuccess, string OrderId, string ErrorMessage)> Finalize3DPaymentCallbackAsync(IFormCollection formData);

        /// <summary>
        /// Siparişi iptal eder (gün sonu yapılmadıysa).
        /// </summary>
        Task<(bool IsSuccess, string ErrorMessage)> CancelPaymentAsync(string orderId);

        /// <summary>
        /// Siparişi iade eder (gün sonu yapıldıysa).
        /// </summary>
        Task<(bool IsSuccess, string ErrorMessage)> RefundPaymentAsync(string orderId);
    }
}