using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

public class MonthlyBudgetConfiguration : IEntityTypeConfiguration<MonthlyBudget>
{
    public void Configure(EntityTypeBuilder<MonthlyBudget> builder)
    {
        builder.HasKey(budget => budget.Id);
        builder.Property(budget => budget.Id).ValueGeneratedNever();
        builder.Property(budget => budget.Allowance).HasPrecision(18, 2);
        builder.Ignore(budget => budget.Spent);
        builder.HasIndex(budget => new { budget.UserId, budget.Year, budget.Month }).IsUnique();

        builder.HasOne(budget => budget.User)
            .WithMany(user => user.MonthlyBudgets)
            .HasForeignKey(budget => budget.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
