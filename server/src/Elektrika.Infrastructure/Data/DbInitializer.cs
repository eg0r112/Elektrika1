using Elektrika.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Elektrika.Infrastructure.Data;

public static class DbInitializer
{
    private const string DefaultAdminUsername = "admin";

    public static async Task InitializeAsync(
        AppDbContext context,
        bool isDevelopment,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        if (!await context.PriceCategories.AnyAsync(cancellationToken))
        {
            await SeedPriceDataAsync(context, cancellationToken);
        }

        if (!await context.AdminUsers.AnyAsync(cancellationToken))
        {
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD");

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                if (isDevelopment)
                {
                    adminPassword = "admin123";
                    logger?.LogWarning("Using default dev admin password. Set ADMIN_INITIAL_PASSWORD for production.");
                }
                else
                {
                    logger?.LogWarning("Admin user was not seeded. Set ADMIN_INITIAL_PASSWORD to create the first admin.");
                    return;
                }
            }

            await SeedAdminUserAsync(context, adminPassword, cancellationToken);
        }
    }

    private static async Task SeedPriceDataAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        var categoryIds = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var (name, sortOrder) in PriceSeedData.Categories)
        {
            var category = new PriceCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                SortOrder = sortOrder,
            };

            categoryIds[name] = category.Id;
            context.PriceCategories.Add(category);
        }

        foreach (var (legacyKey, categoryName, itemName, unit, price, sortOrder) in PriceSeedData.Items)
        {
            context.PriceItems.Add(new PriceItem
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryIds[categoryName],
                LegacyKey = legacyKey,
                Name = itemName,
                Unit = unit,
                Price = price,
                SortOrder = sortOrder,
                IsActive = true,
            });
        }

        foreach (var (legacyKey, label, percent) in PriceSeedData.Surcharges)
        {
            context.Surcharges.Add(new Surcharge
            {
                Id = Guid.NewGuid(),
                LegacyKey = legacyKey,
                Label = label,
                Percent = percent,
                IsActive = true,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAdminUserAsync(
        AppDbContext context,
        string password,
        CancellationToken cancellationToken)
    {
        var passwordHasher = new PasswordHasher<AdminUser>();
        var adminUser = new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = DefaultAdminUsername,
            CreatedAtUtc = DateTime.UtcNow,
        };

        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, password);
        context.AdminUsers.Add(adminUser);

        await context.SaveChangesAsync(cancellationToken);
    }
}
