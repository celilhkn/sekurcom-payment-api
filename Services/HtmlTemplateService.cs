using System;

namespace Sekurcom.Services
{
    // 3d secure ve mock banka için gerekli html şablonlarını oluşturan servis
    public class HtmlTemplateService : IHtmlTemplateService
    {
        public string Generate3DRedirectForm(string formAction, string merchantId, string orderId, decimal amount, string okUrl, string failUrl, string hash)
        {
            return $@"
            <html>
            <head><title>3D Secure Yönlendirme</title></head>
            <body onload=""document.forms[0].submit()"">
                <h2>Banka 3D Secure sayfasına yönlendiriliyorsunuz...</h2>
                <form action=""{formAction}"" method=""POST"">
                    <input type=""hidden"" name=""merchantId"" value=""{merchantId}"" />
                    <input type=""hidden"" name=""orderId"" value=""{orderId}"" />
                    <input type=""hidden"" name=""amount"" value=""{amount}"" />
                    <input type=""hidden"" name=""okUrl"" value=""{okUrl}"" />
                    <input type=""hidden"" name=""failUrl"" value=""{failUrl}"" />
                    <input type=""hidden"" name=""hash"" value=""{hash}"" />
                </form>
            </body>
            </html>";
        }

        public string GenerateMockBank3DPage(string merchantId, string orderId, decimal amount, string okUrl, string failUrl)
        {
            return $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Banka 3D Secure Doğrulama</title>
    <style>
        body {{ margin: 0; font-family: 'Inter', sans-serif; background: #e5e5e5; display: flex; justify-content: center; align-items: center; min-height: 100vh; }}
        .threed-content {{ background: white; width: 100%; max-width: 400px; display: flex; flex-direction: column; box-shadow: 0 0 20px rgba(0,0,0,0.1); }}
        .threed-topbar {{ display: flex; align-items: center; padding: 15px 20px; border-bottom: 1px solid #eee; font-size: 16px; font-weight: 600; color: #333; }}
        .ssl-lock {{ margin-left: auto; display: flex; flex-direction: column; align-items: center; font-size: 9px; color: #00a859; font-weight: bold; }}
        .threed-logos {{ display: flex; justify-content: space-between; align-items: center; padding: 15px 20px; }}
        .logo-guvenli {{ display: flex; align-items: center; gap: 5px; color: #0ea5e9; font-weight: bold; font-size: 14px; }}
        .logo-ziraat {{ display: flex; align-items: center; gap: 8px; font-weight: 600; font-size: 16px; color: #333; }}
        .threed-body {{ padding: 0 20px 30px 20px; flex: 1; }}
        .threed-body h2 {{ text-align: center; font-size: 18px; color: #333; margin: 10px 0 20px 0; font-weight: 600; }}
        .threed-table {{ border-top: 1px solid #ddd; border-bottom: 1px solid #ddd; padding: 15px 0; margin-bottom: 20px; }}
        .threed-row {{ display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 14px; }}
        .threed-label {{ color: #333; font-weight: 600; width: 40%; }}
        .threed-val {{ color: #555; width: 60%; text-align: left; }}
        .threed-desc {{ text-align: center; font-size: 12px; color: #666; line-height: 1.6; margin-bottom: 25px; }}
        .input-code-group {{ margin-bottom: 20px; }}
        .input-code-group label {{ display: block; font-weight: 600; font-size: 15px; color: #333; margin-bottom: 8px; }}
        .input-code-group input {{ width: 100%; padding: 12px; border: 1px solid #ccc; border-radius: 4px; font-size: 16px; outline: none; box-sizing: border-box; }}
        .input-code-group input:focus {{ border-color: #0ea5e9; }}
        .btn-cyan {{ width: 100%; background-color: #0ea5e9; color: white; border: none; padding: 14px; font-size: 16px; font-weight: 600; border-radius: 2px; cursor: pointer; margin-bottom: 20px; box-sizing: border-box; }}
        .threed-footer {{ text-align: center; font-size: 14px; color: #333; font-weight: 600; }}
        .threed-links {{ margin-top: 15px; display: flex; justify-content: center; gap: 20px; }}
        .threed-links a {{ color: #0ea5e9; text-decoration: none; font-weight: 600; font-size: 14px; }}
    </style>
</head>
<body>
    <div class=""threed-content"">
        <div class=""threed-topbar"">
            <svg width=""20"" height=""20"" viewBox=""0 0 24 24"" fill=""none"" stroke=""#333"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"">
                <line x1=""19"" y1=""12"" x2=""5"" y2=""12""></line>
                <polyline points=""12 19 5 12 12 5""></polyline>
            </svg>
            Güvenli Ödeme - 3D
            <div class=""ssl-lock"">
                <svg width=""16"" height=""16"" viewBox=""0 0 24 24"" fill=""none"" stroke=""#00a859"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"">
                    <rect x=""3"" y=""11"" width=""18"" height=""11"" rx=""2"" ry=""2""></rect>
                    <path d=""M7 11V7a5 5 0 0 1 10 0v4""></path>
                </svg>
                SSL secured
            </div>
        </div>

        <div class=""threed-logos"">
            <div class=""logo-guvenli"">
                <svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""#0ea5e9""><circle cx=""12"" cy=""12"" r=""10""/></svg>
                GÜVENLİ ÖDE
            </div>
            <div class=""logo-ziraat"">
                <svg width=""18"" height=""18"" viewBox=""0 0 24 24"" fill=""none"" stroke=""#E1001A"" stroke-width=""3"" stroke-linecap=""round"" stroke-linejoin=""round"">
                    <path d=""M12 2L2 7l10 5 10-5-10-5z""></path>
                    <path d=""M2 17l10 5 10-5""></path>
                    <path d=""M2 12l10 5 10-5""></path>
                </svg>
                Ziraat Bankası
            </div>
        </div>

        <div class=""threed-body"">
            <h2>Doğrulama kodunu giriniz</h2>
            
            <div class=""threed-table"">
                <div class=""threed-row"">
                    <span class=""threed-label"">İşyeri Adı:</span>
                    <span class=""threed-val"">{merchantId}</span>
                </div>
                <div class=""threed-row"">
                    <span class=""threed-label"">İşlem Tutarı:</span>
                    <span class=""threed-val"">{amount:F2} TL</span>
                </div>
                <div class=""threed-row"">
                    <span class=""threed-label"">İşlem Tarihi-Saati:</span>
                    <span class=""threed-val"">{DateTime.Now:dd.MM.yyyy - HH:mm}</span>
                </div>
                <div class=""threed-row"">
                    <span class=""threed-label"">Kart Numarası:</span>
                    <span class=""threed-val"">XXXX XXXX XXXX 5678</span>
                </div>
            </div>
            
            <!-- sms formu (mockbankcontroller/verifysms endpointine post atıyor) -->
            <form action=""/api/MockBank/verify-sms"" method=""POST"">
                <input type=""hidden"" name=""orderId"" value=""{orderId}"" />
                <input type=""hidden"" name=""okUrl"" value=""{okUrl}"" />
                <input type=""hidden"" name=""failUrl"" value=""{failUrl}"" />

                <p class=""threed-desc"">
                    Şifreniz nolu cep telefonunuza gönderilecektir.<br>
                    Referans no: {orderId.Substring(0, 8).ToUpper()}
                </p>

                <div class=""input-code-group"">
                    <label>Doğrulama Kodu</label>
                    <input type=""text"" name=""smsCode"" id=""smsCode"" required autocomplete=""off"" autofocus>
                </div>

                <button type=""submit"" class=""btn-cyan"" id=""btnVerify"">Onayla</button>

                <div class=""threed-footer"">
                    Kalan Süre: 2:52
                    <div class=""threed-links"">
                        <a href=""{failUrl}"">İşlemi İptal Et</a>
                        <a href=""#"">Yardım</a>
                    </div>
                </div>
            </form>
        </div>
    </div>
</body>
</html>";
        }

        public string GenerateMockBankSuccessPage(string okUrl, string orderId, string authCode)
        {
            return $@"
                <html><body onload=""document.forms[0].submit()"">
                    <form action=""{okUrl}"" method=""POST"">
                        <input type=""hidden"" name=""oid"" value=""{orderId}"" />
                        <input type=""hidden"" name=""AuthCode"" value=""{authCode}"" />
                    </form>
                </body></html>";
        }

        public string GenerateMockBankFailPage(string failUrl, string orderId, string errorMessage)
        {
            return $@"
                <html><body onload=""document.forms[0].submit()"">
                    <form action=""{failUrl}"" method=""POST"">
                        <input type=""hidden"" name=""oid"" value=""{orderId}"" />
                        <input type=""hidden"" name=""ErrMsg"" value=""{errorMessage}"" />
                    </form>
                </body></html>";
        }
    }
}
