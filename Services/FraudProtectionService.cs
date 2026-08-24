using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sekurcom.Services
{
    // adam çalıntı kart denerse ip üzerinden banlıyoruz
    public class FraudProtectionService : IFraudProtectionService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<FraudProtectionService> _logger;

        // limit ayarları
        private const int MaxAllowedDifferentCardsPerMinute = 5;
        private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan BanDuration = TimeSpan.FromMinutes(15);

        public FraudProtectionService(IMemoryCache cache, ILogger<FraudProtectionService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public bool RecordAttempt(string ipAddress, string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(cardNumber))
                return false;

            if (IsIpBlacklisted(ipAddress))
                return true;

            string cacheKey = $"fraud_attempts_{ipAddress}";

            // ip için daha önce kaydedilmiş denemeleri getiriyorum yoksa yeni liste oluşturuyorum
            if (!_cache.TryGetValue(cacheKey, out List<AttemptRecord>? attempts) || attempts == null)
            {
                attempts = new List<AttemptRecord>();
            }

            var now = DateTime.UtcNow;

            // 1 dakikadan eski denemeleri siliyorum boşuna yer kaplamasın
            attempts.RemoveAll(a => now - a.Timestamp > WindowDuration);

            // yeni denemeyi ekliyorum
            attempts.Add(new AttemptRecord
            {
                CardNumber = cardNumber.Trim(),
                Timestamp = now
            });

            // önbelleği güncelliyorum 1 dakika içinde silinecek şekilde
            _cache.Set(cacheKey, attempts, WindowDuration);

            // 1 dakika içinde girilen farklı kart sayısına bakıyorum
            int uniqueCardsCount = attempts.Select(a => a.CardNumber).Distinct().Count();

            if (uniqueCardsCount >= MaxAllowedDifferentCardsPerMinute)
            {
                // aynı ip'den çok fazla farklı kart denenirse banlıyorum
                string banKey = $"fraud_ban_{ipAddress}";
                _cache.Set(banKey, true, BanDuration);

                _logger.LogCritical(
                    "[SECURITY ALERT - FRAUD SALDIRISI] IP: {IpAddress} 1 dakika içinde {Count} FARKLI kart numarası ile ödeme denedi! IP adresi {BanMinutes} dakika engellendi.",
                    ipAddress, uniqueCardsCount, BanDuration.TotalMinutes);

                return true;
            }

            return false;
        }

        public bool IsIpBlacklisted(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            string banKey = $"fraud_ban_{ipAddress}";
            return _cache.TryGetValue(banKey, out bool isBanned) && isBanned;
        }

        private class AttemptRecord
        {
            public string CardNumber { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
    }
}
