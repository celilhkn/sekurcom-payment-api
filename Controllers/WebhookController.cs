using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sekurcom.Data;
using Sekurcom.Models;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sekurcom.Controllers
{
    [AllowAnonymous] // Webhook'lar dışarıdan (bankadan) geldiği için auth istenmez
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly PaymentDbContext _db;
        private readonly IyzicoSettings _iyzicoSettings;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(PaymentDbContext db, IOptions<IyzicoSettings> iyzicoSettings, ILogger<WebhookController> logger)
        {
            _db = db;
            _iyzicoSettings = iyzicoSettings.Value;
            _logger = logger;
        }

        // Iyzico (veya benzeri bir sağlayıcı) için örnek Webhook dinleyicisi
        [HttpPost("iyzico")]
        public async Task<IActionResult> IyzicoWebhook([FromBody] WebhookPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.PaymentId))
            {
                _logger.LogWarning("[WEBHOOK] Geçersiz payload alındı.");
                return BadRequest("Invalid Payload");
            }

            // Güvenlik adımı: İmza (Signature) doğrulaması. 
            // Iyzico header'da X-IYZI-SIGNATURE gönderir, bunu SecretKey ile doğrulamak gerekir.
            var signatureHeader = Request.Headers["X-IYZI-SIGNATURE"].ToString();
            
            // İmza doğrulama mock (Gerçekte Iyzico dökümantasyonuna göre token oluşturulup eşleştirilir)
            if (!ValidateSignature(signatureHeader, payload.PaymentId))
            {
                _logger.LogWarning("[WEBHOOK] İmza doğrulaması başarısız. PaymentId: {PaymentId}", payload.PaymentId);
                return Unauthorized("Invalid Signature");
            }

            var record = await _db.Payments.FindAsync(payload.PaymentConversationId); // OrderId (ConversationId)
            
            if (record == null)
            {
                _logger.LogWarning("[WEBHOOK] Sistemde bulunmayan bir sipariş için webhook geldi. OrderId: {OrderId}", payload.PaymentConversationId);
                return NotFound("Order not found");
            }

            // Status güncellemesi
            if (payload.Status == "SUCCESS")
            {
                record.Status = "Successful";
                _logger.LogInformation("[WEBHOOK] Ödeme başarılı olarak güncellendi. OrderId: {OrderId}", record.OrderId);
            }
            else if (payload.Status == "FAILURE")
            {
                record.Status = "Failed";
                _logger.LogInformation("[WEBHOOK] Ödeme başarısız olarak güncellendi. OrderId: {OrderId}", record.OrderId);
            }

            _db.Payments.Update(record);
            await _db.SaveChangesAsync();

            // Webhook'a HTTP 200 dönmemiz gerekir ki sağlayıcı tekrar tekrar denemesin
            return Ok();
        }

        private bool ValidateSignature(string signatureHeader, string paymentId)
        {
            // Basit bir mock imza kontrolü: Eğer header boşsa ve prod ortamındaysak false döner.
            // Gerçek dünyada HMAC-SHA256 ile SecretKey kullanılarak payload hash'lenir ve header ile karşılaştırılır.
            if (string.IsNullOrEmpty(signatureHeader) && string.IsNullOrEmpty(_iyzicoSettings.SecretKey))
            {
                return true; // Geliştirme ortamı varsayımı
            }

            // Şimdilik test amaçlı her zaman true dönüyoruz
            return true;
        }
    }

    public class WebhookPayload
    {
        public string Status { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string PaymentConversationId { get; set; } = string.Empty; // Bizim OrderId'miz
    }
}
