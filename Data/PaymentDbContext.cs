using Sekurcom.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Sekurcom.Data
{

    public class PaymentDbContext : IdentityDbContext<IdentityUser>
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        public DbSet<PaymentRecord> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<PaymentRecord>(eb =>
            {
                eb.HasKey(e => e.OrderId);
                eb.Property(e => e.OrderId).HasMaxLength(100);
                eb.Property(e => e.Status).HasMaxLength(50);
                eb.Property(e => e.BankResponse).HasColumnType("TEXT");
            });
        }
    }
}