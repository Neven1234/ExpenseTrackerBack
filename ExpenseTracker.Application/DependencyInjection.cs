using ExpenseTracker.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<MonthlyBudgetService>();
        services.AddScoped<ExpenseService>();

        return services;
    }
}
