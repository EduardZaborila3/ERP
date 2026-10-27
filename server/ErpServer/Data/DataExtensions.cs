using System.Text;
using ErpServer.Models;
using ErpServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ErpServer.Data;

public static class DataExtensions
{
    public static void AddErpDb(this WebApplicationBuilder builder)
    {
        var connString = builder.Configuration.GetConnectionString("SupabaseDb");
        
        builder.Services.AddDbContext<ErpDbContext>(options =>
            options.UseNpgsql(connString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null
                );
            })
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
        );
    }

    public static void AddErpCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowReactClient", policy =>
            {
                policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });
    }

    public static void AddErpAuth(this WebApplicationBuilder builder)
    {
        var secret = builder.Configuration["JwtSettings:Secret"] ?? "SuperSecretEnterpriseErpJwtSigningKey2026!LongKeyForSecurity";
        var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "ErpServer";
        var audience = builder.Configuration["JwtSettings:Audience"] ?? "ErpClient";

        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        builder.Services.AddScoped<ITokenService, TokenService>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        builder.Services.AddAuthorization();
    }

    public static async Task MigrateAndSeedDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        await dbContext.Database.MigrateAsync();

        if (!await dbContext.Roles.AnyAsync())
        {
            dbContext.Roles.AddRange(
                new Models.AppRole { Id = 1, Name = "Admin" },
                new Models.AppRole { Id = 2, Name = "Inginer" }
            );
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.JobTitles.AnyAsync())
        {
            dbContext.JobTitles.AddRange(
                new Models.JobTitle { Id = 1, Name = "Manager" },
                new Models.JobTitle { Id = 2, Name = "Inginer" }
            );
            await dbContext.SaveChangesAsync();
        }

        // Ensure "Șofer" job title exists
        var soferTitle = await dbContext.JobTitles
            .FirstOrDefaultAsync(jt => jt.Name.ToLower() == "șofer" || jt.Name.ToLower() == "sofer");
        if (soferTitle == null)
        {
            soferTitle = new Models.JobTitle { Name = "Șofer" };
            dbContext.JobTitles.Add(soferTitle);
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Sites.AnyAsync())
        {
            dbContext.Sites.Add(new Models.ConstructionSite
            {
                Name = "Baza Tomești",
                Description = "Baza principală de unde pleacă toate echipamentele și materialele",
                Location = "Tomești, Iași",
                IsBase = true,
                CreatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var managerUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == "user@example.com");

        if (managerUser == null)
        {
            var admin = new Models.User
            {
                Email = "user@example.com",
                FirstName = "Admin",
                LastName = "Test",
                AppRoleId = 1,
                JobTitleId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin123!");
            dbContext.Users.Add(admin);
            await dbContext.SaveChangesAsync();
        }
        else if (string.IsNullOrEmpty(managerUser.PasswordHash))
        {
            // Seed password for existing user if missing
            managerUser.PasswordHash = passwordHasher.HashPassword(managerUser, "Admin123!");
            await dbContext.SaveChangesAsync();
        }

        // Ensure at least one driver user exists
        var driverUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == "sofer@example.com");
        if (driverUser == null && soferTitle != null)
        {
            var driver = new Models.User
            {
                Email = "sofer@example.com",
                FirstName = "Vasile",
                LastName = "Dumitru",
                AppRoleId = 2,
                JobTitleId = soferTitle.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            driver.PasswordHash = passwordHasher.HashPassword(driver, "Sofer123!");
            dbContext.Users.Add(driver);
            await dbContext.SaveChangesAsync();
        }

        // Seed the 5 construction sites and 100 mock stocks if not already seeded
        await StockMockSeeder.SeedMockDataAsync(dbContext, force: false);
    }
}
