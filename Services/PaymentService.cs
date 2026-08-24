using Sekurcom.Data;
using Sekurcom.Models;
using Sekurcom.Providers;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sekurcom.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PaymentDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPaymentProvider _paymentProvider;
        private readonly IHtmlTemplateService _htmlTemplateService;

        public PaymentService(
            PaymentDbContext db, 
            IHttpContextAccessor httpContextAccessor, 
            IPaymentProvider paymentProvider, 
            IHtmlTemplateService htmlTemplateService)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _paymentProvider = paymentProvider;
            _htmlTemplateService = htmlTemplateService;
        }

        public async Task<(bool IsSuccess, string OrderId, object Body, int StatusCode)> ExecutePaymentAsync(PaymentRequestDto request, string? userId)
        {
            string orderId = Guid.NewGuid().ToString();

            var req = _httpContextAccessor.HttpContext?.Request;
            string baseUrl = req != null ? $"{req.Scheme}://{req.Host}" : "https://localhost:7285";

            string basariliDonusAdresi = $"{baseUrl}/api/payment/success";
            string hataliDonusAdresi = $"{baseUrl}/api/payment/fail";

            var result = await _paymentProvider.ExecutePaymentAsync(request, orderId, basariliDonusAdresi, hataliDonusAdresi);

            var status = result.IsSuccess ? "Successful" : "Failed";
            
            var storedRecord = new PaymentRecord 
            { 
                UserId = userId, 
                OrderId = orderId, 
                Status = result.StatusCode == 500 ? "System Error" : status, 
                Amount = request.Amount, 
                BankResponse = JsonSerializer.Serialize(result.BankResponse), 
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                CustomerAddress = request.CustomerAddress,
                PurchasedItems = request.PurchasedItems,
                CreatedAt = DateTime.UtcNow 
            };

            _db.Payments.Add(storedRecord);
            await _db.SaveChangesAsync();

            return (result.IsSuccess, orderId, storedRecord, result.StatusCode);
        }

        public async Task<PaymentRecord?> GetPaymentByIdAsync(string id)
        {
            return await _db.Payments.FindAsync(id);
        }

        public async Task<List<PaymentRecord>> GetAllPaymentsAsync()
        {
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(_db.Payments);
        }

        public async Task<(bool IsSuccess, string HtmlContent, string ErrorMessage)> Initialize3DPaymentAsync(PaymentRequestDto request, string? userId)
        {
            string orderId = Guid.NewGuid().ToString();

            var req = _httpContextAccessor.HttpContext?.Request;
            string baseUrl = req != null ? $"{req.Scheme}://{req.Host}" : "https://localhost:7285";

            // iyzico için tek url yetiyor ziraat için iki tane lazım biz ikisini de aynı yere basıyoruz
            // banka callback ile sonucu buraya yolluyor
            string basariliDonusAdresi = $"{baseUrl}/api/payment/callback";
            string hataliDonusAdresi = $"{baseUrl}/api/payment/callback";

            var result = await _paymentProvider.Initialize3DPaymentAsync(request, orderId, basariliDonusAdresi, hataliDonusAdresi);

            var paymentRecord = new PaymentRecord
            {
                UserId = userId,
                OrderId = orderId,
                Amount = request.Amount,
                Status = result.IsSuccess ? "Pending3D" : "Failed",
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                CustomerAddress = request.CustomerAddress,
                PurchasedItems = request.PurchasedItems,
                CreatedAt = DateTime.UtcNow,
                BankResponse = result.IsSuccess ? "{ \"Message\": \"3D Secure başlatıldı.\" }" : JsonSerializer.Serialize(new { Error = result.ErrorMessage })
            };
            _db.Payments.Add(paymentRecord);
            await _db.SaveChangesAsync();

            // Return the tuple
            return result;
        }

        public async Task<(bool IsSuccess, string OrderId, string ErrorMessage)> Finalize3DPaymentCallbackAsync(IFormCollection formData)
        {
            // iyzico parametreleri
            string status = formData["status"]; // success or failure
            string paymentId = formData["paymentId"];
            string conversationData = formData["conversationData"];
            string conversationId = formData["conversationId"]; // orderid'yi buraya yollamıştık
            
            // ziraat parametreleri
            string orderId = formData["oid"];
            string mdStatus = formData["mdStatus"];
            string authCode = formData["AuthCode"];
            string errMsg = formData["ErrMsg"];

            // dönen sonuca göre hangi provider olduğunu anlıyoruz
            bool isIyzico = !string.IsNullOrEmpty(paymentId);
            
            string finalOrderId = isIyzico ? conversationId : orderId;
            string finalCallback1 = isIyzico ? paymentId : mdStatus;
            string finalCallback2 = isIyzico ? conversationData : authCode;

            if (string.IsNullOrEmpty(finalOrderId))
            {
                return (false, "", "Geçersiz callback parametreleri (OrderId bulunamadı)");
            }

            var record = await _db.Payments.FindAsync(finalOrderId);
            if (record == null) return (false, finalOrderId, "Sipariş bulunamadı");

            // auth işlemi öncesi iyzico başarısız durum kontrolü
            if (isIyzico && status != "success")
            {
                var mdErrorVal = formData["mdErrorMsg"];
                string mdError = string.IsNullOrEmpty(mdErrorVal) ? "3D Secure Doğrulaması Başarısız" : mdErrorVal.ToString();
                record.Status = "Failed";
                record.BankResponse = JsonSerializer.Serialize(new { Message = "3D Secure Hata", Error = mdError });
                _db.Payments.Update(record);
                await _db.SaveChangesAsync();
                return (false, finalOrderId, mdError);
            }

            // auth işlemi öncesi ziraat başarısız durum kontrolü
            if (!isIyzico && (mdStatus == "0" || mdStatus == "2" || mdStatus == "3" || mdStatus == "4" || mdStatus == "5" || mdStatus == "6" || mdStatus == "9"))
            {
                record.Status = "Failed";
                record.BankResponse = JsonSerializer.Serialize(new { Message = "3D Secure Hata", Error = errMsg ?? "3D Doğrulama Başarısız" });
                _db.Payments.Update(record);
                await _db.SaveChangesAsync();
                return (false, finalOrderId, errMsg ?? "3D Doğrulama Başarısız");
            }

            // ödemeyi sonlandıran auth işlemi
            var finalResult = await _paymentProvider.Finalize3DPaymentAsync(finalOrderId, finalCallback1, finalCallback2);

            if (finalResult.IsSuccess)
            {
                record.Status = "Successful";
                record.BankResponse = JsonSerializer.Serialize(finalResult.BankResponse);
                _db.Payments.Update(record);
                await _db.SaveChangesAsync();
                return (true, finalOrderId, "");
            }
            else
            {
                record.Status = "Failed";
                record.BankResponse = JsonSerializer.Serialize(finalResult.BankResponse);
                _db.Payments.Update(record);
                await _db.SaveChangesAsync();
                return (false, finalOrderId, "Ödeme onaylanamadı (ThreedsAuth başarısız)");
            }
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> CancelPaymentAsync(string orderId)
        {
            var record = await _db.Payments.FindAsync(orderId);
            if (record == null) return (false, "Sipariş bulunamadı");
            if (record.Status != "Successful" && record.Status != "Pending3D") return (false, "Sipariş iptal edilebilir durumda değil");

            var result = await _paymentProvider.CancelPaymentAsync(orderId);
            if (result.IsSuccess)
            {
                record.Status = "Cancelled";
                _db.Payments.Update(record);
                await _db.SaveChangesAsync();
            }

            return result;
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> RefundPaymentAsync(string orderId)
        {
            var record = await _db.Payments.FindAsync(orderId);
            if (record == null) return (false, "Sipariş bulunamadı");
            if (record.Status != "Successful") return (false, "Sipariş iade edilebilir durumda değil");

            var result = await _paymentProvider.RefundPaymentAsync(orderId, record.Amount);
            if (result.IsSuccess)
            {
                record.Status = "Refunded";
                _db.Payments.Update(record);
                await _db.SaveChangesAsync();
            }

            return result;
        }
    }
}