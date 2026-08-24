---
name: celil_custom_rules
description: Projeye özel C# kodlama, mimari ve dokümantasyon standartları.
---

# Kurumsal Kodlama Kuralları

1. **Açıklayıcı Türkçe Dokümantasyon:**
   Yazılan tüm servislerin, denetleyicilerin (Controller) ve karmaşık iş mantıklarının üzerine temiz Türkçe XML dokümantasyonu (`/// <summary>`) veya açıklama satırları ekle.

2. **Clean Code & Mimari Düzen:**
   - Bağımlılıkları (Dependency Injection) daima constructor (kurucu metot) üzerinden en üstte enjekte et.
   - Değişken isimlerinde anlaşılır İngilizce isimler kullan (`IsBlacklisted`, `AttemptsCount`).

3. **Temel Mimari ve Güvenlik Kuralları:**
    - Controller sınıflarına doğrudan iş mantığı veya DbContext sorgusu yazma; tüm mantığı Services katmanında topla.
    - Sabit (hardcoded) URL veya gizli anahtar kullanma; tüm yapılandırmaları appsettings.json veya IHttpContextAccessor üzerinden al.
    - Kullanıcıya ait veri işlemlerinde asla client'tan gelen UserId'ye güvenme; kullanıcı kimliğini her zaman JWT Claims üzerinden çek.
    - Kredi kartı ve CVV gibi hassas verileri loglama ve hata mesajlarında dışarı sızdırma.
    - Veritabanı yönetiminde EnsureCreated yerine her zaman EF Core Migrations standardını uygula.
