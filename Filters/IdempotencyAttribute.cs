using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Sekurcom.Filters
{
    // ödeme yaparken çift tıklama sorununu çözmek için yazdığım filter
    [AttributeUsage(AttributeTargets.Method)]
    public class IdempotencyAttribute : Attribute, IAsyncActionFilter
    {
        private const string IdempotencyHeader = "X-Idempotency-Key";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // header'dan key'i alıyorum
            if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyHeader, out var idempotencyKey) || string.IsNullOrWhiteSpace(idempotencyKey))
            {
                // key yoksa hata fırlatıyorum
                context.Result = new BadRequestObjectResult(new { Durum = "Error", Mesaj = $"{IdempotencyHeader} başlığı (header) zorunludur." });
                return;
            }

            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            var cacheKey = $"Idempotency_{idempotencyKey}";

            // cache'de var mı bakıyorum
            if (cache.TryGetValue(cacheKey, out IActionResult cachedResult))
            {
                if (cachedResult == null)
                {
                    // null ise işlem hala sürüyor demektir adam çift tıklamıştır
                    context.Result = new ConflictObjectResult(new { Durum = "Conflict", Mesaj = "Bu işlem şu anda gerçekleştiriliyor, lütfen bekleyin." });
                    return;
                }
                
                // daha önce bittiyse direkt eski sonucu dönüyorum bankaya tekrar gitmiyor
                context.Result = cachedResult;
                return;
            }

            // işlem yeni başlıyor cache e null koyup kilitliyorum
            // 5 dakika içinde aynı key ile gelenler bekleyin uyarısı alacak
            cache.Set<IActionResult>(cacheKey, null, TimeSpan.FromMinutes(5));

            // asıl controller çalışıyor
            var executedContext = await next();

            // işlem bitince sonuca bakıyorum
            if (executedContext.Exception != null)
            {
                // hata olduysa cachei siliyorum ki tekrar deneyebilsin
                cache.Remove(cacheKey);
            }
            else if (executedContext.Result != null)
            {
                // işlem başarılıysa sonucu 24 saat önbellekte tutuyorum
                cache.Set(cacheKey, executedContext.Result, TimeSpan.FromHours(24));
            }
        }
    }
}
