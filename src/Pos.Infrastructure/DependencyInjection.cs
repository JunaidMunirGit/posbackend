using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pos.Infrastructure.Abstractions.Persistence;
using Pos.Infrastructure.Persistence;
using System;

namespace Pos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("Default")));

        services.AddScoped<IAppDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        return services;
    }
}
