using Sekurcom.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sekurcom.Helpers
{
    /// <summary>
    /// Karalistedeki (Blacklisted) IP adreslerinin isteğini 403 Forbidden ile kesen güvenlik middleware'i.
    /// </summary>
    public class FraudProtectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FraudProtectionMiddleware> _logger;

        public FraudProtectionMiddleware(RequestDelegate next, ILogger<FraudProtectionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IFraudProtectionService fraudService)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (fraudService.IsIpBlacklisted(ipAddress))
            {
                _logger.LogWarning("[SECURITY BLOCKED] Karalistedeki IP'den gelen istek reddedildi! IP: {IpAddress}, Path: {Path}", ipAddress, context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";

                var responsePayload = new
                {
                    Durum = "Forbidden",
                    Mesaj = "Güvenlik Politikası İhlali: Şüpheli kart denemeleri nedeniyle IP adresiniz geçici olarak engellenmiştir.",
                    Ip = ipAddress
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(responsePayload));
                return;
            }

            await _next(context);
        }
    }
}
