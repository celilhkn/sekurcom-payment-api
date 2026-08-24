using System.ComponentModel.DataAnnotations;

namespace Sekurcom.Models
{
    // ödeme yaparken frontendden gelen veriler burada toplanıyor
    public class PaymentRequestDto
    {
        // kart sahibinin adı soyadı
        [Required(ErrorMessage = "Kart üzerindeki isim zorunludur.")]
        public string CardHolderName { get; set; }

        // boşluksuz 16 haneli kart numarası
        [Required(ErrorMessage = "Kart numarası zorunludur.")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Kart numarası tam 16 haneli olmalıdır.")]
        public string CardNumber { get; set; }

        // son kullanma ayı
        [Required(ErrorMessage = "Son kullanma ayı zorunludur.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])$", ErrorMessage = "Ay bilgisi 01 ile 12 arasında olmalıdır.")]
        public string ExpireMonth { get; set; }

        // son kullanma yılı 2 hane de olur 4 hane de
        [Required(ErrorMessage = "Son kullanma yılı zorunludur.")]
        [StringLength(4, MinimumLength = 2, ErrorMessage = "Yıl bilgisi 2 veya 4 haneli olmalıdır (Örn: 26 veya 2026).")]
        public string ExpireYear { get; set; }

        // arkadaki 3 haneli kod
        [Required(ErrorMessage = "CVV zorunludur.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "CVV tam 3 haneli olmalıdır.")]
        public string Cvv { get; set; }

        // ne kadar çekeceğiz
        [Required(ErrorMessage = "Ödeme tutarı zorunludur.")]
        [Range(0.1, 100000, ErrorMessage = "Ödeme tutarı 0'dan büyük olmalıdır.")]
        public decimal Amount { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? PurchasedItems { get; set; }
    }
}