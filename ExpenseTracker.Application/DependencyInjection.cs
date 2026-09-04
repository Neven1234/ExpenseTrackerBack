using ExpenseTracker.Application.Persistence;
using ExpenseTracker.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<AuthService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<MonthlyBudgetService>();
        services.AddScoped<ExpenseService>();

        return services;
    }
}
