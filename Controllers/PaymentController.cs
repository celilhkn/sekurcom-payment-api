using Sekurcom.Filters;
using Sekurcom.Models;
using Sekurcom.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Text.Json;

namespace Sekurcom.Controllers
{
    // ödeme isteklerini ve 3d secure yönlendirmelerini burada karşılıyorum
    [Authorize]
    [EnableRateLimiting("IpRateLimit")]
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IFraudProtectionService _fraudProtectionService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService, 
            IFraudProtectionService fraudProtectionService, 
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _fraudProtectionService = fraudProtectionService;
            _logger = logger;
        }

        // 3d secure olmadan direkt ödeme çekmek için kullandığım yer
        [HttpPost("payments")]
        [Idempotency]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentRequestDto request)
        {
            if (request == null) return BadRequest(new { Durum = "Error", Mesaj = "İstek gövdesi boş." });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            bool isBlocked = _fraudProtectionService.RecordAttempt(ip, request.CardNumber);
            if (isBlocked)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Durum = "Forbidden", Mesaj = "Şüpheli kart denemeleri nedeniyle ödeme engellendi." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var exec = await _paymentService.ExecutePaymentAsync(request, userId);

            if (exec.IsSuccess)
            {
                _logger.LogInformation("[ÖDEME BAŞARILI] OrderId: {OrderId}, Tutar: {Amount} TL, Kart: ****{Last4}", exec.OrderId, request.Amount, request.CardNumber[^4..]);
                return Created($"/api/payment/payments/{exec.OrderId}", exec.Body);
            }

            _logger.LogWarning("[ÖDEME BAŞARISIZ] OrderId: {OrderId}, Tutar: {Amount} TL, StatusCode: {Code}", exec.OrderId, request.Amount, exec.StatusCode);
            return StatusCode(exec.StatusCode, exec.Body);
        }

        // sipariş id'sine göre ödemeyi getiriyor başkasının ödemesini göremesinler diye kontrol ekledim
        [HttpGet("payments/{id}")]
        public async Task<IActionResult> GetPayment(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { Durum = "Error", Mesaj = "Id boş olamaz." });

            var record = await _paymentService.GetPaymentByIdAsync(id);

            if (record == null) return NotFound(new { Durum = "NotFound", Mesaj = "Payment not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");

            if (record.UserId != userId && !isAdmin)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Durum = "Forbidden", Mesaj = "Bu ödemeyi görüntüleme yetkiniz yok." });
            }

            object bankObj = record.BankResponse;
            try { bankObj = JsonSerializer.Deserialize<JsonElement>(record.BankResponse); } catch { }

            return Ok(new { orderId = record.OrderId, durum = record.Status, bankaMesaji = bankObj, amount = record.Amount, createdAt = record.CreatedAt });
        }

        // db'deki bütün ödemeleri çeken yer admin paneli tablosu için lazım
        [Authorize(Roles = "Admin")]
        [HttpGet("payments/all")]
        public async Task<IActionResult> GetAllPayments()
        {
            var payments = await _paymentService.GetAllPaymentsAsync();
            return Ok(payments);
        }

        // müşterinin sadece kendi geçmiş siparişlerini görmesi için
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var payments = await _paymentService.GetAllPaymentsAsync();
            // normalde bu işi db tarafında yapmak lazım ama şimdilik hafızada filtreliyorum
            var myPayments = payments.Where(p => p.UserId == userId).OrderByDescending(p => p.CreatedAt).ToList();
            return Ok(myPayments);
        }

        // 3d secure akışını başlatan yer iyzico'dan dönen html formunu frontend'e iletiyoruz
        [HttpPost("pay-3d")]
        public async Task<IActionResult> ProcessPayment3D([FromBody] PaymentRequestDto request)
        {
            if (request == null) return BadRequest(new { Durum = "Error", Mesaj = "İstek gövdesi boş." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("[3D SECURE BAŞLATILDI] Tutar: {Amount} TL, Kart: ****{Last4}", request.Amount, request.CardNumber[^4..]);
            
            var result = await _paymentService.Initialize3DPaymentAsync(request, userId);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("[3D SECURE BAŞARISIZ] Hata: {Error}", result.ErrorMessage);
                return BadRequest(new { Durum = "Error", Mesaj = result.ErrorMessage });
            }

            // frontend tarafı base64 çözüp ekrana basacak html içeriğini json ile dönüyoruz
            return Ok(new { Durum = "Success", HtmlContent = result.HtmlContent });
        }

        // global error handler düzgün çalışıyor mu diye test için yazdım bunu
        [HttpGet("test-error")]
        public IActionResult TestErrorHandling()
        {
            // hata denemesi
            throw new Exception("Bu, sistemdeki kritik bir hatanın (Örn: Veritabanı çökmesi) simülasyonudur.");
        }

        // banka veya iyzico işini bitirince bize buradan sonucu yolluyor
        [AllowAnonymous]
        [HttpPost("callback")]
        public async Task<IActionResult> PaymentCallback([FromForm] IFormCollection formData)
        {
            var result = await _paymentService.Finalize3DPaymentCallbackAsync(formData);

            if (result.IsSuccess)
            {
                _logger.LogInformation("[3D SECURE BAŞARILI] OrderId: {OrderId}", result.OrderId);
                return Redirect($"/success.html?orderId={result.OrderId}");
            }
            else
            {
                _logger.LogWarning("[3D SECURE BAŞARISIZ] OrderId: {OrderId}, Hata: {Error}", result.OrderId, result.ErrorMessage);
                return Redirect($"/fail.html?orderId={result.OrderId}&errorMessage={Uri.EscapeDataString(result.ErrorMessage ?? string.Empty)}");
            }
        }

        // İptal (Cancel) endpointi (Sadece Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> CancelPayment(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { Durum = "Error", Mesaj = "Id boş olamaz." });

            var result = await _paymentService.CancelPaymentAsync(id);
            if (result.IsSuccess)
            {
                _logger.LogInformation("[İPTAL BAŞARILI] OrderId: {OrderId}", id);
                return Ok(new { Durum = "Success", Mesaj = "Ödeme iptal edildi." });
            }

            _logger.LogWarning("[İPTAL BAŞARISIZ] OrderId: {OrderId}, Hata: {Error}", id, result.ErrorMessage);
            return BadRequest(new { Durum = "Error", Mesaj = result.ErrorMessage });
        }

        // İade (Refund) endpointi (Sadece Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost("refund/{id}")]
        public async Task<IActionResult> RefundPayment(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { Durum = "Error", Mesaj = "Id boş olamaz." });

            var result = await _paymentService.RefundPaymentAsync(id);
            if (result.IsSuccess)
            {
                _logger.LogInformation("[İADE BAŞARILI] OrderId: {OrderId}", id);
                return Ok(new { Durum = "Success", Mesaj = "Ödeme iade edildi." });
            }

            _logger.LogWarning("[İADE BAŞARISIZ] OrderId: {OrderId}, Hata: {Error}", id, result.ErrorMessage);
            return BadRequest(new { Durum = "Error", Mesaj = result.ErrorMessage });
        }
    }
}