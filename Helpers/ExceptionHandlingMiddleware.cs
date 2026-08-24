using System.Net;
using System.Text.Json;

namespace Sekurcom.Helpers
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sistemde beklenmeyen bir hata oluştu. Hata Detayı: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var result = JsonSerializer.Serialize(new
            {
                Durum = "Error",
                Mesaj = "Sunucu tarafında beklenmeyen bir hata meydana geldi.",
                HataDetayi = exception.Message // Sadece geliştirme ortamında göstermek daha güvenlidir, ancak demo amaçlı bırakıldı.
            });

            return context.Response.WriteAsync(result);
        }
    }
}
