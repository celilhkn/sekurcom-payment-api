using Sekurcom.Services;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Sekurcom.Controllers
{
    // iyzico kullanmadan önce ziraat denemek için yazdığım sahte banka. 
    // sms falan soruyor sanki gerçek bankaymış gibi çalışıyor.
    [Route("api/[controller]")]
    [ApiController]
    public class MockBankController : ControllerBase
    {
        private readonly IHtmlTemplateService _htmlTemplateService;

        public MockBankController(IHtmlTemplateService htmlTemplateService)
        {
            _htmlTemplateService = htmlTemplateService;
        }

        // sahte 3d secure ekranını frontend'e basıyoruz
        [HttpPost("3d-page")]
        public IActionResult Render3DPage([FromForm] string merchantId, [FromForm] string orderId, [FromForm] decimal amount, [FromForm] string okUrl, [FromForm] string failUrl)
        {
            var html = _htmlTemplateService.GenerateMockBank3DPage(merchantId, orderId, amount, okUrl, failUrl);
            return Content(html, "text/html", System.Text.Encoding.UTF8);
        }

        // kullanıcının girdiği kodu kontrol eden yer. 000000 girerse patlıyor diğer durumlarda ok
        [HttpPost("verify-sms")]
        public IActionResult VerifySms([FromForm] string orderId, [FromForm] string smsCode, [FromForm] string okUrl, [FromForm] string failUrl)
        {
            // testi patlatmak istersem diye 000000 kuralını koymuştum
            if (smsCode == "000000")
            {
                var failHtml = _htmlTemplateService.GenerateMockBankFailPage(failUrl, orderId, "SMS Doğrulaması Hatalı!");
                return Content(failHtml, "text/html", System.Text.Encoding.UTF8);
            }

            // başarılı olursa sanki banka onay vermiş gibi random auth kodu atıyorum
            string mockAuthCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            var successHtml = _htmlTemplateService.GenerateMockBankSuccessPage(okUrl, orderId, mockAuthCode);
            
            return Content(successHtml, "text/html", System.Text.Encoding.UTF8);
        }
    }
}
