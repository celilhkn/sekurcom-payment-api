using Sekurcom.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sekurcom.Controllers
{
    // user login register falan burada dönüyor jwt token falan basıyoruz
    [EnableRateLimiting("IpRateLimit")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        // db'ye yeni adam eklemek için. default customer rolü veriyorum kimse kafasına göre admin olmasın
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            var user = new IdentityUser { UserName = request.Email, Email = request.Email };
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("[KAYIT BAŞARISIZ] E-posta: {Email} — Hatalar: {Errors}", request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors);
            }

            // herkes customer olarak başlıyor admin yapmak istersem db den el ile güncellerim
            string assignedRole = "Customer";

            if (!await _roleManager.RoleExistsAsync(assignedRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(assignedRole));
            }

            await _userManager.AddToRoleAsync(user, assignedRole);

            _logger.LogInformation("[KAYIT BAŞARILI] E-posta: {Email}, Rol: {Role}", request.Email, assignedRole);
            return Ok(new { Mesaj = $"User successfully registered with {assignedRole} role." });
        }

        // login denemesi yapanları buradan geçirip token veriyorum brute force için lockout aktif
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            // adam gerçekten db'de var mı bakıyorum yoksa boşuna uğraşmayalım
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser == null)
            {
                _logger.LogWarning("[GİRİŞ BAŞARISIZ] Kayıtlı olmayan e-posta ile giriş denemesi: {Email}", request.Email);
                return BadRequest(new { Mesaj = "Invalid email or password!" });
            }

            // 5 kere yanlış girerse 15dk ban yiyecek şekilde signin managerı ayarladım
            var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, isPersistent: false, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                _logger.LogWarning("[HESAP KİLİTLENDİ] E-posta: {Email} — 5 başarısız giriş sonrası 15 dk kilitlendi.", request.Email);
                return StatusCode(429, new { Mesaj = "Account locked for 15 minutes due to too many failed attempts." });
            }

            if (!result.Succeeded)
            {
                var failedCount = await _userManager.GetAccessFailedCountAsync(existingUser);
                _logger.LogWarning("[GİRİŞ BAŞARISIZ] E-posta: {Email} — Hatalı deneme sayısı: {Count}/5", request.Email, failedCount);
                return BadRequest(new { Mesaj = "Invalid email or password!", FailedAttempts = $"{failedCount}/5" });
            }

            // başarılı olursa rolleri çekip içine ekleyeceğim tokenı oluşturuyorum
            var user = await _userManager.FindByEmailAsync(request.Email);
            var roles = await _userManager.GetRolesAsync(user!);

            // giriş başarılı token basalım
            var tokenString = GenerateJwtToken(user!, roles);

            _logger.LogInformation("[GİRİŞ BAŞARILI] E-posta: {Email}, Roller: {Roles}", user!.Email, string.Join(", ", roles));
            return Ok(new
            {
                Mesaj = "Login successful! Welcome.",
                Kullanici = user!.Email,
                Yetkiler = roles,
                Token = tokenString //  ürettiğim tokenı adama veriyorum frontende bunu kullanacak
            });
        }

        // token üretmek için yazdığım minik yardımcı metod jwt ayarlarını appsettings den çekiyor
        private string GenerateJwtToken(IdentityUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}