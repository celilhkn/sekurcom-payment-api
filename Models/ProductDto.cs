using System;

namespace Sekurcom.Models
{
    /// <summary>
    /// DummyJSON API'den dönen kök yanıt yapısı (pagination ve listeyi içerir).
    /// </summary>
    public class DummyJsonProductResponse
    {
        public List<ProductDto> Products { get; set; } = new List<ProductDto>();
        public int Total { get; set; }
        public int Skip { get; set; }
        public int Limit { get; set; }
    }

    /// <summary>
    /// DummyJSON'dan dönen ürün verilerini temsil eden DTO.
    /// </summary>
    public class ProductDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Thumbnail { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new List<string>();
        public decimal Rating { get; set; }
        public int Stock { get; set; }
    }
}
