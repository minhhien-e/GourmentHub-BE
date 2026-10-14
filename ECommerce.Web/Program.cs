using Ecommerce.Abstraction.Services;
using Ecommerce.Domain.Models.Contracts.RedisInMemoryRepository;
using Ecommerce.Domain.Models.Contracts.Repository;
using Ecommerce.Domain.Models.Contracts.Seed;
using Ecommerce.Domain.Models.Contracts.UOW;
using Ecommerce.Domain.Models.Identity;
using Ecommerce.Presistence.Contexts;
using Ecommerce.Presistence.Data_Seed;
using Ecommerce.Presistence.Repository;
using Ecommerce.Presistence.UnitOfWork;
using Ecommerce.Service.businessServices;
using Ecommerce.Service.MappingProfiles;
using Ecommerce.Shared.Common.ErrorModels;
using ECommerce.Web.Custom_MiddleWares;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

namespace ECommerce.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ─── MVC & API Explorer ──────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ─── Redis ───────────────────────────────────────────────────
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(
                    builder.Configuration.GetConnectionString("RedisConnection")!
                )
            );

            // ─── Database Contexts ────────────────────────────────────────
            builder.Services.AddDbContext<StoreIdntityDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection"))
            );

            builder.Services.AddDbContext<StoreDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );

            // ─── Application Services ─────────────────────────────────────
            builder.Services.AddScoped<IdataSeed, DataSeeeding>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddAutoMapper(m =>
                m.AddProfile(new ProjectProfiles(builder.Configuration))
            );
            builder.Services.AddScoped<IServiceManger, ServiceManger>();

            // ─── Repositories & Cache ─────────────────────────────────────
            builder.Services.AddScoped<ICartRepo, CartRepo>();
            builder.Services.AddScoped<ICacheRepo, CacheRepo>();
            builder.Services.AddScoped<ICacheService, CacheService>();

            // ─── Validation Error Response ────────────────────────────────
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value!.Errors.Any())
                        .Select(e => new PramValidationError
                        {
                            Field = e.Key,
                            Errors = e.Value!.Errors.Select(er => er.ErrorMessage)
                        });

                    return new BadRequestObjectResult(new PramValidationsReturn { Errors = errors });
                };
            });

            // ─── Identity ─────────────────────────────────────────────────
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<StoreIdntityDbContext>();

            // ─── Authentication (JWT + Google OAuth) ──────────────────────
            builder.Services.AddAuthentication(config =>
            {
                config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                config.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
                    ?? throw new InvalidOperationException("Google ClientId is not configured.");

                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
                    ?? throw new InvalidOperationException("Google ClientSecret is not configured.");

                options.CallbackPath = "/signin-google";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = builder.Configuration["JWTOptions:Issuer"],
                    ValidateAudience         = true,
                    ValidAudience            = builder.Configuration["JWTOptions:Audience"],
                    ValidateLifetime         = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["JWTOptions:SecurityKey"]
                            ?? throw new InvalidOperationException("JWT SecurityKey is not configured.")
                        )
                    )
                };
            });

            var app = builder.Build();

            // ─── Forwarded Headers (reverse proxy / MonsterASP) ───────────
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // ─── Database Seeding ─────────────────────────────────────────
            await SeedDatabase(app);

            // ─── Swagger ──────────────────────────────────────────────────
            app.UseSwagger();
            app.UseSwaggerUI(c =>
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "GourmetHub API v1")
            );

            // ─── Middleware Pipeline ──────────────────────────────────────
            app.UseMiddleware<CustomExceptionMiddleWare>();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            await app.RunAsync();
        }

        /// <summary>Seeds both the Identity and App databases with initial data.</summary>
        private static async Task SeedDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbInitializer = scope.ServiceProvider.GetRequiredService<IdataSeed>();
            await dbInitializer.DataSeedAsync();
            await dbInitializer.IdentityInitializAsync();
        }
    }
}
