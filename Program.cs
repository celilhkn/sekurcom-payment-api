using Sekurcom.Data;
using Sekurcom.Providers;
using Sekurcom.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore;
using Serilog;
using Sekurcom.Helpers;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Sekurcom
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Logları takip etmek için serilog kurdum konsola ve dosyaya yazsın
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/payment-api-log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            builder.Host.UseSerilog();

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IFraudProtectionService, FraudProtectionService>();
            builder.Services.AddSingleton<IHtmlTemplateService, HtmlTemplateService>();
            // Ziraat'e dönmek istersem IyzicoPaymentProvider yerine ZiraatPaymentProvider yazarım buraya
            builder.Services.AddScoped<IPaymentProvider, IyzicoPaymentProvider>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IFakeStoreService, FakeStoreService>();
            builder.Services.AddEndpointsApiExplorer();

            // Biri gelip saniyede 1000 istek atıp sunucuyu çökertmesin diye rate limiter koydum
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // IP başına dakikada 50 istek sınırı bence yeterli
                options.AddPolicy("IpRateLimit", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 50,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            // Swagger arayüzü ayarları
            builder.Services.AddSwaggerGen(c =>
            {
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "Lütfen 'Bearer ' kelimesinden sonra boşluk bırakıp Token'ınızı yapıştırın.",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>() 
        }
    });
            });

            // PostgreSQL veritabanı bağlantısı (Docker konteynerinde çalışıyor)
            builder.Services.AddDbContext<PaymentDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            );
            // Kullanıcı giriş çıkış ve rol ayarları
            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;

                // Biri şifre denemesi yapıp durmasın diye 5 yanlışta hesabı 15 dk kilitliyorum
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<PaymentDbContext>()
            .AddDefaultTokenProviders();

            // Token doğrulama ayarları
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero, // Microsoft'un verdiği 5 dk opsiyonunu sıfırladım token anında ölsün
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };
            });

            builder.Services.AddHttpClient();
            builder.Services.Configure<Sekurcom.Models.ZiraatPosSettings>(builder.Configuration.GetSection("ZiraatPosSettings"));
            builder.Services.Configure<Sekurcom.Models.IyzicoSettings>(builder.Configuration.GetSection("IyzicoSettings"));

            // Tarayıcıdan apiye istek atarken patlamamak için cors açtım
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("İzin",
                    policy => policy.AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .AllowAnyHeader());
            });

            var app = builder.Build();

            // Uygulama başlarken db yoksa kursun ve ilk verileri atsın uğraştırmasın
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                db.Database.Migrate();
                DbSeeder.SeedPayments(db);
                await DbSeeder.SeedUsersAsync(scope.ServiceProvider);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sekurcom v1");
                    c.RoutePrefix = "swagger"; // UI at /swagger
                });
            }
            var env = app.Environment.EnvironmentName;
            Console.WriteLine($"{env}");
            
            // Hataları yakalama ve şüpheli IP'leri banlama işlemleri
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<FraudProtectionMiddleware>();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("İzin");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
