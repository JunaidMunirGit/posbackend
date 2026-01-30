using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application.Abstractions.Security;
using Pos.Domain.Entities;
using Pos.Infrastructure.Abstractions.Persistence;
using Pos.Infrastructure.Persistence;
using Pos.Infrastructure.Security;

namespace Pos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("Default")));

        // Application sees only the interface, Infrastructure provides the implementation
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Services used by CQRS handlers
        services.AddScoped<IAuthService, AuthService>();


        return services;
    }
}
