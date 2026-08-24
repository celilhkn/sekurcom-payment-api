using System;

namespace Sekurcom.Models
{
    // db'de tuttuğumuz ödeme kayıt tablosu
    public class PaymentRecord
    {
        // ödemeyi yapanın id'si
        public string? UserId { get; set; }

        // sipariş veya işlem numarası (guid veriyorum genelde)
        public string OrderId { get; set; } = string.Empty;

        // işlemin durumu başarılı başarısız vs
        public string Status { get; set; } = string.Empty;

        // çekilen para
        public decimal Amount { get; set; }

        // bankadan dönen ham cevabı buraya gömüyorum lazım olur
        public string BankResponse { get; set; } = string.Empty; // JSON string

        // müşteri adı soyadı
        public string? CustomerName { get; set; }

        // müşteri telefonu
        public string? CustomerPhone { get; set; }

        // müşteri adresi
        public string? CustomerAddress { get; set; }

        // sepetteki ürünlerin json hali
        public string? PurchasedItems { get; set; }

        // kayıt zamanı
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
