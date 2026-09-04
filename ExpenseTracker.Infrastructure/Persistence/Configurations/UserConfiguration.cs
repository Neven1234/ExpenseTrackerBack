using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Email).IsRequired().HasMaxLength(256);
        builder.Property(user => user.Username).IsRequired().HasMaxLength(100);
        builder.Property(user => user.PasswordHash).IsRequired().HasMaxLength(512);
        builder.HasIndex(user => user.Email).IsUnique();
    }
}
