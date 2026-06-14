using Elektrika.Application.Interfaces;
using Elektrika.Application.Options;
using Elektrika.Infrastructure.Background;
using Elektrika.Infrastructure.Data;
using Elektrika.Infrastructure.Messaging;
using Elektrika.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elektrika.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IOrderNotificationPublisher, RabbitMqOrderNotificationPublisher>();

        services.AddScoped<IPriceService, PriceService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddHttpClient<ITelegramNotifier, TelegramNotifier>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static IServiceCollection AddOrderNotificationWorkers(this IServiceCollection services)
    {
        services.AddHostedService<OrderNotificationConsumer>();
        services.AddHostedService<OrderNotificationRepublishWorker>();
        return services;
    }
}
