using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Abstractions.Security;

public interface ITokenGenerator
{
    AccessToken Generate(User user);
}
