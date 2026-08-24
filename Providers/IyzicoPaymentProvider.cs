using Sekurcom.Models;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sekurcom.Providers
{
    public class IyzicoPaymentProvider : IPaymentProvider
    {
        private readonly Iyzipay.Options _options;

        public IyzicoPaymentProvider(IOptions<IyzicoSettings> settings)
        {
            _options = new Iyzipay.Options
            {
                ApiKey = settings.Value.ApiKey,
                SecretKey = settings.Value.SecretKey,
                BaseUrl = settings.Value.BaseUrl
            };
        }

        public async Task<(bool IsSuccess, object BankResponse, int StatusCode)> ExecutePaymentAsync(PaymentRequestDto request, string orderId, string basariliDonusAdresi, string hataliDonusAdresi)
        {
            var paymentRequest = new CreatePaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = orderId,
                Price = request.Amount.ToString("F2").Replace(',', '.'),
                PaidPrice = request.Amount.ToString("F2").Replace(',', '.'),
                Currency = Currency.TRY.ToString(),
                Installment = 1,
                BasketId = "B" + orderId,
                PaymentChannel = PaymentChannel.WEB.ToString(),
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                PaymentCard = new PaymentCard
                {
                    CardHolderName = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName : "Test User",
                    CardNumber = request.CardNumber?.Replace(" ", "").Trim() ?? "",
                    ExpireMonth = request.ExpireMonth?.Trim() ?? "",
                    ExpireYear = (request.ExpireYear?.Length == 4 ? request.ExpireYear.Substring(2) : request.ExpireYear)?.Trim() ?? "",
                    Cvc = request.Cvv?.Trim() ?? "",
                    RegisterCard = 0
                },
                Buyer = new Buyer
                {
                    Id = "BY789",
                    Name = "John",
                    Surname = "Doe",
                    GsmNumber = "+905350000000",
                    Email = "email@email.com",
                    IdentityNumber = "74300864791",
                    LastLoginDate = "2015-10-05 12:43:35",
                    RegistrationDate = "2013-04-21 15:12:09",
                    RegistrationAddress = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
                    Ip = "85.34.78.112",
                    City = "Istanbul",
                    Country = "Turkey",
                    ZipCode = "34732"
                },
                ShippingAddress = new Address
                {
                    ContactName = "Jane Doe",
                    City = "Istanbul",
                    Country = "Turkey",
                    Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
                    ZipCode = "34742"
                },
                BillingAddress = new Address
                {
                    ContactName = "Jane Doe",
                    City = "Istanbul",
                    Country = "Turkey",
                    Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
                    ZipCode = "34742"
                },
                BasketItems = new List<BasketItem>
                {
                    new BasketItem
                    {
                        Id = "BI101",
                        Name = "Test Product",
                        Category1 = "Collectibles",
                        ItemType = BasketItemType.PHYSICAL.ToString(),
                        Price = request.Amount.ToString("F2").Replace(',', '.')
                    }
                }
            };

            var payment = await Payment.Create(paymentRequest, _options);

            bool isSuccess = payment.Status == "success";
            return (isSuccess, (object)payment, isSuccess ? 200 : 400);
        }

        public async Task<(bool IsSuccess, string HtmlContent, string ErrorMessage)> Initialize3DPaymentAsync(PaymentRequestDto request, string orderId, string basariliDonusAdresi, string hataliDonusAdresi)
        {
            var initRequest = new CreatePaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = orderId,
                Price = request.Amount.ToString("F2").Replace(',', '.'),
                PaidPrice = request.Amount.ToString("F2").Replace(',', '.'),
                Currency = Currency.TRY.ToString(),
                Installment = 1,
                BasketId = "B" + orderId,
                PaymentChannel = PaymentChannel.WEB.ToString(),
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                CallbackUrl = basariliDonusAdresi,
                PaymentCard = new PaymentCard
                {
                    CardHolderName = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName : "Test User",
                    CardNumber = request.CardNumber?.Replace(" ", "").Trim() ?? "",
                    ExpireMonth = request.ExpireMonth?.Trim() ?? "",
                    ExpireYear = (request.ExpireYear?.Length == 4 ? request.ExpireYear.Substring(2) : request.ExpireYear)?.Trim() ?? "",
                    Cvc = request.Cvv?.Trim() ?? "",
                    RegisterCard = 0
                },
                Buyer = new Buyer
                {
                    Id = "BY789",
                    Name = "John",
                    Surname = "Doe",
                    GsmNumber = "+905350000000",
                    Email = "email@email.com",
                    IdentityNumber = "74300864791",
                    LastLoginDate = "2015-10-05 12:43:35",
                    RegistrationDate = "2013-04-21 15:12:09",
                    RegistrationAddress = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
                    Ip = "85.34.78.112",
                    City = "Istanbul",
                    Country = "Turkey",
                    ZipCode = "34732"
                },
                ShippingAddress = new Address
                {
                    ContactName = "Jane Doe",
                    City = "Istanbul",
                    Country = "Turkey",
                    Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
                    ZipCode = "34742"
                },
                BillingAddress = new Address
                {
                    ContactName = "Jane Doe",
                    City = "Istanbul",
                    Country = "Turkey",
                    Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
                    ZipCode = "34742"
                },
                BasketItems = new List<BasketItem>
                {
                    new BasketItem
                    {
                        Id = "BI101",
                        Name = "Test Product",
                        Category1 = "Collectibles",
                        ItemType = BasketItemType.PHYSICAL.ToString(),
                        Price = request.Amount.ToString("F2").Replace(',', '.')
                    }
                }
            };

            var initialize = await ThreedsInitialize.Create(initRequest, _options);

            if (initialize.Status == "success")
            {
                string base64Html = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(initialize.HtmlContent));
                return (true, base64Html, "");
            }

            string errorDesc = !string.IsNullOrEmpty(initialize.ErrorMessage) ? initialize.ErrorMessage : (initialize.ErrorCode ?? "Bilinmeyen hata");
            return (false, "", $"Iyzico Hatası: {errorDesc}");
        }

        public async Task<(bool IsSuccess, object BankResponse, int StatusCode)> Finalize3DPaymentAsync(string orderId, string callbackData1, string callbackData2)
        {
            var authRequest = new CreateThreedsPaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = callbackData2,
                PaymentId = callbackData1
            };

            var auth = await ThreedsPayment.Create(authRequest, _options);
            bool isSuccess = auth.Status == "success";

            return (isSuccess, (object)auth, isSuccess ? 200 : 400);
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> CancelPaymentAsync(string orderId)
        {
            var cancelRequest = new CreateCancelRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = Guid.NewGuid().ToString(),
                PaymentId = orderId, // Varsayım: Db'de sakladığımız callbackData1 (paymentId) buraya orderId olarak geliyor
                Ip = "85.34.78.112"
            };

            var cancel = await Cancel.Create(cancelRequest, _options);
            return (cancel.Status == "success", cancel.ErrorMessage ?? "");
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> RefundPaymentAsync(string orderId, decimal amount)
        {
            var refundRequest = new CreateRefundRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = Guid.NewGuid().ToString(),
                PaymentTransactionId = orderId, // Varsayım
                Price = amount.ToString("F2").Replace(',', '.'),
                Ip = "85.34.78.112",
                Currency = Currency.TRY.ToString()
            };

            var refund = await Refund.Create(refundRequest, _options);
            return (refund.Status == "success", refund.ErrorMessage ?? "");
        }
    }
}
