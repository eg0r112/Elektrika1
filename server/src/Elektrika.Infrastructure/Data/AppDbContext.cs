using Elektrika.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elektrika.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<PriceCategory> PriceCategories => Set<PriceCategory>();

    public DbSet<PriceItem> PriceItems => Set<PriceItem>();

    public DbSet<Surcharge> Surcharges => Set<Surcharge>();

    public DbSet<OrderRequest> OrderRequests => Set<OrderRequest>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceCategory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(e => e.SortOrder);

            entity.HasMany(e => e.PriceItems)
                .WithOne(e => e.Category)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PriceItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.LegacyKey)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Price)
                .HasPrecision(18, 2);

            entity.HasIndex(e => e.LegacyKey)
                .IsUnique();

            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<Surcharge>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.LegacyKey)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Label)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(e => e.LegacyKey)
                .IsUnique();
        });

        modelBuilder.Entity<OrderRequest>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Message)
                .HasMaxLength(4000);

            entity.HasIndex(e => e.TelegramStatus);

            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.SurchargeTotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.VisitFee)
                .HasPrecision(18, 2);

            entity.Property(e => e.Total)
                .HasPrecision(18, 2);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAtUtc);

            entity.HasIndex(e => e.ClientRequestId)
                .IsUnique()
                .HasFilter("\"ClientRequestId\" IS NOT NULL");
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasIndex(e => e.Username)
                .IsUnique();
        });
    }
}
