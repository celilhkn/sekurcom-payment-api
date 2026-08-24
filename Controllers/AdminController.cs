using ClosedXML.Excel;
using Sekurcom.Data;
using Sekurcom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

namespace Sekurcom.Controllers
{
    // Burası Admin Paneline sadece adminlerin girebilmesi için kurduğum kontrol mekanizması. Şimdilik listeleme, excele dökme falan var.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly PaymentDbContext _dbContext;

        public AdminController(PaymentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Bütün ödeme geçmişini admin panelinde tabloya dökmek için verileri çektiğim endpoint
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var transactions = await _dbContext.Payments
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(transactions);
        }

        // Muhasebeci falan Excel ister diye ekledim. Siparişleri indirip excel dosyası veriyor.
        [HttpGet("transactions/export")]
        public async Task<IActionResult> ExportTransactionsToExcel()
        {
            var transactions = await _dbContext.Payments
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("İşlem Geçmişi");

            // Excel sütun başlıkları
            worksheet.Cell(1, 1).Value = "Kullanıcı ID (E-posta)";
            worksheet.Cell(1, 2).Value = "Müşteri Adı Soyadı";
            worksheet.Cell(1, 3).Value = "Müşteri Telefonu";
            worksheet.Cell(1, 4).Value = "Sipariş No";
            worksheet.Cell(1, 5).Value = "Durum";
            worksheet.Cell(1, 6).Value = "Tutar";
            worksheet.Cell(1, 7).Value = "Tarih";
            worksheet.Cell(1, 8).Value = "Banka Yanıtı";
            worksheet.Cell(1, 9).Value = "Satın Alınan Ürünler";
            worksheet.Cell(1, 10).Value = "Teslimat Adresi";

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            // DB'den gelen verileri satır satır excele basıyoruz
            int row = 2;
            foreach (var t in transactions)
            {
                worksheet.Cell(row, 1).Value = t.UserId ?? "Anonim";
                worksheet.Cell(row, 2).Value = t.CustomerName ?? "-";
                worksheet.Cell(row, 3).Value = t.CustomerPhone ?? "-";
                worksheet.Cell(row, 4).Value = t.OrderId;
                worksheet.Cell(row, 5).Value = t.Status;
                worksheet.Cell(row, 6).Value = t.Amount;
                worksheet.Cell(row, 7).Value = t.CreatedAt.ToString("g");
                worksheet.Cell(row, 8).Value = t.BankResponse;
                worksheet.Cell(row, 9).Value = t.PurchasedItems ?? "-";
                worksheet.Cell(row, 10).Value = t.CustomerAddress ?? "-";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "IslemGecmisi.xlsx"
            );
        }


        // Ana sayfadaki o afilli grafikleri ve ciro kartlarını dolduran istatistik api'si
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var payments = await _dbContext.Payments.ToListAsync();
            
            var totalRevenue = payments.Where(p => p.Status == "Successful").Sum(p => p.Amount);
            var totalSales = payments.Count(p => p.Status == "Successful");
            var successRate = payments.Count > 0 
                ? (double)totalSales / payments.Count * 100 
                : 0;

            // Son 7 günün satış trendi grafiği için tarihleri ayarlıyorum
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                .Reverse()
                .ToList();

            var dailySales = last7Days.Select(date => new
            {
                Date = date.ToString("dd MMM"),
                Revenue = payments
                    .Where(p => p.Status == "Successful" && p.CreatedAt.Date == date)
                    .Sum(p => p.Amount)
            }).ToList();

            return Ok(new
            {
                TotalRevenue = totalRevenue,
                TotalSales = totalSales,
                SuccessRate = successRate,
                TotalTransactions = payments.Count,
                DailySales = dailySales
            });
        }

        // Müşteri arayıp paramı iade et derse admin panelinden basıp iptal etmemiz için kurduğum basit refund yapısı
        [HttpPost("refund/{id}")]
        public async Task<IActionResult> RefundPayment(string id)
        {
            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == id);
            if (payment == null) return NotFound(new { Mesaj = "İşlem bulunamadı." });

            if (payment.Status != "Successful")
            {
                return BadRequest(new { Mesaj = "Sadece başarılı işlemler iade edilebilir." });
            }

            payment.Status = "Refunded";
            payment.BankResponse = "{\"Message\":\"İade Başarılı\", \"RefundId\":\"" + Guid.NewGuid().ToString().Substring(0, 8) + "\"}";
            
            await _dbContext.SaveChangesAsync();

            return Ok(new { Mesaj = "İade işlemi başarıyla gerçekleştirildi." });
        }
    }
}
