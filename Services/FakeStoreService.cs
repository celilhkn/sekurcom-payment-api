using Sekurcom.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Sekurcom.Services
{
    // fakestore api'den ürünleri çeken servis sınıfı
    public class FakeStoreService : IFakeStoreService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FakeStoreService> _logger;
        // dummyjson base url (ürünler endpointi)
        private const string BaseUrl = "https://dummyjson.com/products";

        public FakeStoreService(HttpClient httpClient, ILogger<FakeStoreService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<DummyJsonProductResponse> GetAllProductsAsync(int skip = 0, int limit = 30)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<DummyJsonProductResponse>($"{BaseUrl}?skip={skip}&limit={limit}");
                return response ?? new DummyJsonProductResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DummyJSON API'den ürünler çekilirken bir hata oluştu.");
                return new DummyJsonProductResponse();
            }
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ProductDto>($"{BaseUrl}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DummyJSON API'den {ProductId} ID'li ürün çekilirken bir hata oluştu.", id);
                return null;
            }
        }

        public async Task<DummyJsonProductResponse> SearchProductsAsync(string query, int skip = 0, int limit = 30)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<DummyJsonProductResponse>($"{BaseUrl}/search?q={Uri.EscapeDataString(query)}&skip={skip}&limit={limit}");
                return response ?? new DummyJsonProductResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DummyJSON API'de '{Query}' araması yapılırken hata oluştu.", query);
                return new DummyJsonProductResponse();
            }
        }

        public async Task<DummyJsonProductResponse> GetProductsByCategoryAsync(string category, int skip = 0, int limit = 30)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<DummyJsonProductResponse>($"{BaseUrl}/category/{Uri.EscapeDataString(category)}?skip={skip}&limit={limit}");
                return response ?? new DummyJsonProductResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DummyJSON API'den '{Category}' kategorisi çekilirken hata oluştu.", category);
                return new DummyJsonProductResponse();
            }
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            try
            {
                // dummyjson kategorileri artık obje listesi veya string dönebiliyor 
                // gelen formata göre parse edip string listesine çeviriyorum
                var elements = await _httpClient.GetFromJsonAsync<List<System.Text.Json.JsonElement>>($"{BaseUrl}/categories");
                if (elements == null) return new List<string>();

                var categories = new List<string>();
                foreach (var el in elements)
                {
                    if (el.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        categories.Add(el.GetString()!);
                    }
                    else if (el.ValueKind == System.Text.Json.JsonValueKind.Object && el.TryGetProperty("slug", out var slugProp))
                    {
                        categories.Add(slugProp.GetString()!);
                    }
                }
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DummyJSON API'den kategoriler çekilirken hata oluştu.");
                return new List<string>();
            }
        }
    }
}
