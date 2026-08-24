using Sekurcom.Models;
using Sekurcom.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sekurcom.Controllers
{
    // fakestore api'den ürünleri çektiğim yer frontend buradan besleniyor
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IFakeStoreService _fakeStoreService;

        public ProductsController(IFakeStoreService fakeStoreService)
        {
            _fakeStoreService = fakeStoreService;
        }

        // filtreleme falan varsa onlara da bakıp dönüyorum yoksa hepsini veriyorum
        [HttpGet]
        public async Task<ActionResult<DummyJsonProductResponse>> GetAllProducts(
            [FromQuery] string? q, 
            [FromQuery] string? category, 
            [FromQuery] int skip = 0, 
            [FromQuery] int limit = 30)
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                return Ok(await _fakeStoreService.SearchProductsAsync(q, skip, limit));
            }
            
            if (!string.IsNullOrWhiteSpace(category))
            {
                return Ok(await _fakeStoreService.GetProductsByCategoryAsync(category, skip, limit));
            }

            var products = await _fakeStoreService.GetAllProductsAsync(skip, limit);
            return Ok(products);
        }

        // kategori filtreleri için lazım oluyor
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _fakeStoreService.GetCategoriesAsync();
            return Ok(categories);
        }

        // ürün detay sayfasına girince çalışan yer
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _fakeStoreService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound(new { Mesaj = "Ürün bulunamadı." });
            }

            return Ok(product);
        }
    }
}
