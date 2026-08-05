using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Services;
using InternalOperations.Application.Services;
using InternalOperations.Infrastructure;
using InternalOperations.Persistence;
using InternalOperations.Persistence.Abstractions;
using InternalOperations.Persistence.Context;
using InternalOperations.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }

    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString(provider) ?? configuration["ConnectionStrings:DefaultConnection"];

        services.AddHttpContextAccessor();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("InternalOperations-Development");
                return;
            }

            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
