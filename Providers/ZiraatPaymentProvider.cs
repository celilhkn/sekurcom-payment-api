using Sekurcom.Helpers;
using Sekurcom.Models;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sekurcom.Providers
{
    // ziraat sanal pos entegrasyonu
    public class ZiraatPaymentProvider : IPaymentProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ZiraatPosSettings _posSettings;

        public ZiraatPaymentProvider(IHttpClientFactory httpClientFactory, IOptions<ZiraatPosSettings> posSettings)
        {
            _httpClientFactory = httpClientFactory;
            _posSettings = posSettings.Value;
        }

        public async Task<(bool IsSuccess, object BankResponse, int StatusCode)> ExecutePaymentAsync(PaymentRequestDto request, string orderId, string basariliDonusAdresi, string hataliDonusAdresi)
        {
            var ziraatApiUrl = _posSettings.ApiUrl;
            var client = _httpClientFactory.CreateClient();

            string generatedHash = PaymentHelper.CreateZiraatHash(
                _posSettings.MerchantId, orderId, request.Amount.ToString("F2"),
                basariliDonusAdresi, hataliDonusAdresi, _posSettings.HashKey);

            var bankRequestPayload = new
            {
                merchantId = _posSettings.MerchantId,
                orderId = orderId,
                amount = request.Amount,
                cardNumber = request.CardNumber,
                expireDate = request.ExpireMonth + "/" + request.ExpireYear,
                cvv = request.Cvv,
                okUrl = basariliDonusAdresi,
                failUrl = hataliDonusAdresi,
                hash = generatedHash
            };

            var content = new StringContent(JsonSerializer.Serialize(bankRequestPayload), Encoding.UTF8, "application/json");

            if (request.Amount == 9999)
            {
                var msg = "Yetersiz Bakiye (Hata Kodu: 51)";
                return (false, new { Message = msg }, 400);
            }

            try
            {
                var response = await client.PostAsync(ziraatApiUrl, content);
                var result = await response.Content.ReadAsStringAsync();

                object bankResponse;
                try { bankResponse = JsonSerializer.Deserialize<JsonElement>(result); }
                catch { bankResponse = result; }

                var statusCode = response.IsSuccessStatusCode ? 200 : 400;

                return (response.IsSuccessStatusCode, bankResponse, statusCode);
            }
            catch (Exception ex)
            {
                return (false, new { Exception = ex.Message }, 500);
            }
        }

        public async Task<(bool IsSuccess, string HtmlContent, string ErrorMessage)> Initialize3DPaymentAsync(PaymentRequestDto request, string orderId, string basariliDonusAdresi, string hataliDonusAdresi)
        {
            var ziraat3dUrl = _posSettings.ApiUrl.Replace("/payment", "/3dpayment");

            string generatedHash = PaymentHelper.CreateZiraatHash(
                _posSettings.MerchantId, orderId, request.Amount.ToString("F2"),
                basariliDonusAdresi, hataliDonusAdresi, _posSettings.HashKey);

            // formu otomatik submit edecek html kodu hazırlıyorum
            string htmlForm = $@"
                <!DOCTYPE html>
                <html>
                <head><title>Ziraat 3D Secure</title></head>
                <body onload='document.getElementById(""ziraat3DForm"").submit();'>
                    <form id='ziraat3DForm' action='{ziraat3dUrl}' method='POST'>
                        <input type='hidden' name='merchantId' value='{_posSettings.MerchantId}' />
                        <input type='hidden' name='orderId' value='{orderId}' />
                        <input type='hidden' name='amount' value='{request.Amount.ToString("F2")}' />
                        <input type='hidden' name='cardNumber' value='{request.CardNumber}' />
                        <input type='hidden' name='expireDate' value='{request.ExpireMonth}/{request.ExpireYear}' />
                        <input type='hidden' name='cvv' value='{request.Cvv}' />
                        <input type='hidden' name='okUrl' value='{basariliDonusAdresi}' />
                        <input type='hidden' name='failUrl' value='{hataliDonusAdresi}' />
                        <input type='hidden' name='hash' value='{generatedHash}' />
                        <input type='hidden' name='transactionType' value='Auth' />
                        <noscript>
                            <p>Tarayıcınız yönlendirmeyi desteklemiyor, lütfen butona tıklayın.</p>
                            <button type='submit'>Ödemeye Devam Et</button>
                        </noscript>
                    </form>
                </body>
                </html>";

            // ziraat direkt html istiyor ama ben iyzico ile aynı formatta olsun diye base64 e çevirip basıyorum
            string base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlForm));

            return await Task.FromResult((true, base64Html, ""));
        }

        public async Task<(bool IsSuccess, object BankResponse, int StatusCode)> Finalize3DPaymentAsync(string orderId, string callbackData1, string callbackData2)
        {
            // ziraat mock u için 3d callback'i mdstatus=1 dönüyor ikinci bir auth çağrısına gerek yok
            // bu bir mock implementasyonu o yüzden direkt ok veriyorum
            if (callbackData1 == "1" || callbackData1 == "7" || callbackData1 == "8") 
            {
                // başarılı 3d
                return await Task.FromResult((true, (object)new { Message = "3D Success Mock" }, 200));
            }
            return await Task.FromResult((false, (object)new { Message = "3D Failed Mock" }, 400));
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> CancelPaymentAsync(string orderId)
        {
            // Ziraat için iptal endpointine HTTP POST atılır. Mock olduğu için başarılı dönüyoruz.
            return await Task.FromResult((true, ""));
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> RefundPaymentAsync(string orderId, decimal amount)
        {
            // Ziraat için iade endpointine HTTP POST atılır. Mock olduğu için başarılı dönüyoruz.
            return await Task.FromResult((true, ""));
        }
    }
}
