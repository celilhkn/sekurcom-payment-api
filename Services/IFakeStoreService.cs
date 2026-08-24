using Sekurcom.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sekurcom.Services
{
    /// <summary>
    /// DummyJSON API (eski adıyla FakeStore API) ürün getirme servisinin arayüzü.
    /// </summary>
    public interface IFakeStoreService
    {
        /// <summary>
        /// Tüm ürünleri getirir (Sayfalamalı).
        /// </summary>
        Task<DummyJsonProductResponse> GetAllProductsAsync(int skip = 0, int limit = 30);

        /// <summary>
        /// ID'sine göre tek bir ürün getirir.
        /// </summary>
        Task<ProductDto?> GetProductByIdAsync(int id);

        /// <summary>
        /// Arama kelimesine göre ürünleri getirir.
        /// </summary>
        Task<DummyJsonProductResponse> SearchProductsAsync(string query, int skip = 0, int limit = 30);

        /// <summary>
        /// Belirli bir kategorideki ürünleri getirir.
        /// </summary>
        Task<DummyJsonProductResponse> GetProductsByCategoryAsync(string category, int skip = 0, int limit = 30);

        /// <summary>
        /// Tüm kategorilerin listesini getirir.
        /// </summary>
        Task<IEnumerable<string>> GetCategoriesAsync();
    }
}
