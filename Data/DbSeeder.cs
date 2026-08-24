using Sekurcom.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Sekurcom.Data
{
    public static class DbSeeder
    {
        public static void SeedPayments(PaymentDbContext context)
        {
            if (context.Payments.Any())
            {
                return; // Veritabanında veri (sahte veya gerçek) varsa silme, öylece bırak
            }

            var random = new Random(42); // Seed for reproducible random results
            var dummyPayments = new System.Collections.Generic.List<PaymentRecord>();
            
            string[] names = { "James Wilson", "Emily Carter", "Michael Brown", "Sarah Davis", "Robert Miller", "Jessica Taylor", "Daniel Anderson", "Ashley Thomas", "Matthew Jackson", "Olivia White", "William Harris", "Sophia Martin", "David Thompson", "Isabella Garcia", "Joseph Martinez", "Mia Robinson", "Andrew Clark", "Emma Rodriguez", "Joshua Lewis", "Ava Lee" };
            string[] domains = { "example.com", "mail.com", "test.org", "demo.net" };
            string[] statuses = { "Successful", "Successful", "Successful", "Failed", "Pending3D" };
            string[] addresses = {
                "123 Main St, Apt 4B, New York, NY 10001",
                "456 Oak Avenue, Suite 200, Los Angeles, CA 90012",
                "789 Elm Street, Chicago, IL 60614",
                "1010 Pine Road, Houston, TX 77002",
                "2020 Maple Drive, Phoenix, AZ 85001"
            };
            
            var fakeProducts = new[]
            {
                new { name = "Philips HD9252 Airfryer - Black", price = 89.99m },
                new { name = "Logitech G305 LIGHTSPEED Wireless Mouse", price = 39.99m },
                new { name = "Stanley Classic Thermos 1.0L Green", price = 44.95m },
                new { name = "Apple iPhone 15 128 GB - Black", price = 799.00m },
                new { name = "Samsung 55CU7000 55\" 4K Smart LED TV", price = 347.99m },
                new { name = "Nike Air Force 1 '07 Sneakers", price = 109.99m }
            };

            for (int i = 1; i <= 30; i++)
            {
                var fullName = names[random.Next(names.Length)];
                var emailPrefix = fullName.Split(' ')[0].ToLowerInvariant();
                var domain = domains[random.Next(domains.Length)];
                var status = statuses[random.Next(statuses.Length)];
                
                // Select 1 to 3 random products
                int itemCount = random.Next(1, 4);
                var items = new System.Collections.Generic.List<object>();
                decimal totalAmount = 0;
                for (int j = 0; j < itemCount; j++)
                {
                    var product = fakeProducts[random.Next(fakeProducts.Length)];
                    int qty = random.Next(1, 3);
                    items.Add(new { name = product.name, price = product.price, quantity = qty });
                    totalAmount += product.price * qty;
                }
                
                var createdTime = DateTime.UtcNow.AddDays(-random.Next(1, 30)).AddHours(-random.Next(1, 24));
                var phone = $"(555) {random.Next(100, 999)}-{random.Next(1000, 9999)}";
                var address = addresses[random.Next(addresses.Length)];
                
                string bankResponse;
                if (status == "Successful") {
                    bankResponse = $"{{\"Message\":\"Transaction Approved\", \"AuthCode\":\"{random.Next(100000, 999999)}\"}}";
                } else if (status == "Failed") {
                    string[] errors = { "Insufficient Funds", "Suspected Lost/Stolen Card", "Transaction Declined by Bank", "Card Expiry Date Invalid" };
                    bankResponse = $"{{\"ErrorMessage\":\"{errors[random.Next(errors.Length)]}\", \"ErrorCode\":\"{random.Next(10, 99)}\"}}";
                } else {
                    bankResponse = "{\"Message\":\"Awaiting 3D Verification\", \"Status\":\"3D_REQUIRED\"}";
                }

                dummyPayments.Add(new PaymentRecord
                {
                    OrderId = $"TRX-99{i:D3}",
                    UserId = $"{emailPrefix}@{domain}",
                    Amount = Math.Round(totalAmount, 2),
                    Status = status,
                    CreatedAt = createdTime,
                    BankResponse = bankResponse,
                    CustomerName = fullName,
                    CustomerPhone = phone,
                    CustomerAddress = address,
                    PurchasedItems = System.Text.Json.JsonSerializer.Serialize(items)
                });
            }

            context.Payments.AddRange(dummyPayments);
            context.SaveChanges();
        }

        public static async System.Threading.Tasks.Task SeedUsersAsync(System.IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

            // Rolleri oluştur
            string[] roles = { "Admin", "Customer" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(roleName));
                }
            }

            // Test müşteri hesabı: test@eticaret.com / Password123
            if (await userManager.FindByEmailAsync("test@eticaret.com") == null)
            {
                var testUser = new Microsoft.AspNetCore.Identity.IdentityUser
                {
                    UserName = "test@eticaret.com",
                    Email = "test@eticaret.com",
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(testUser, "Password123");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "Customer");
                }
            }

            // Admin hesabı: admin@admin.com / Admin123!
            if (await userManager.FindByEmailAsync("admin@admin.com") == null)
            {
                var adminUser = new Microsoft.AspNetCore.Identity.IdentityUser
                {
                    UserName = "admin@admin.com",
                    Email = "admin@admin.com",
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Yedek Admin hesabı: admin@eticaret.com / Password123
            if (await userManager.FindByEmailAsync("admin@eticaret.com") == null)
            {
                var adminUser2 = new Microsoft.AspNetCore.Identity.IdentityUser
                {
                    UserName = "admin@eticaret.com",
                    Email = "admin@eticaret.com",
                    EmailConfirmed = true
                };
                var createResult2 = await userManager.CreateAsync(adminUser2, "Password123");
                if (createResult2.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser2, "Admin");
                }
            }
        }
    }
}
