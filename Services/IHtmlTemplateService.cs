namespace Sekurcom.Services
{
    /// <summary>
    /// HTML şablonları oluşturmaktan sorumlu servis arayüzü. (SRP Prensipleri Gereği)
    /// </summary>
    public interface IHtmlTemplateService
    {
        /// <summary>
        /// Bankanın 3D Secure ekranına yönlendirme yapan otomatik form HTML'ini üretir.
        /// </summary>
        string Generate3DRedirectForm(string formAction, string merchantId, string orderId, decimal amount, string okUrl, string failUrl, string hash);
        
        /// <summary>
        /// (Mock) Banka 3D Secure doğrulama sayfasının HTML'ini üretir.
        /// </summary>
        string GenerateMockBank3DPage(string merchantId, string orderId, decimal amount, string okUrl, string failUrl);
        
        /// <summary>
        /// (Mock) Banka başarılı işlem yönlendirme HTML'ini üretir.
        /// </summary>
        string GenerateMockBankSuccessPage(string okUrl, string orderId, string authCode);
        
        /// <summary>
        /// (Mock) Banka hatalı işlem yönlendirme HTML'ini üretir.
        /// </summary>
        string GenerateMockBankFailPage(string failUrl, string orderId, string errorMessage);
    }
}
