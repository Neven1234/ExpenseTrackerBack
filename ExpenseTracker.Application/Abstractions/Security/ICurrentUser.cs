namespace ExpenseTracker.Application.Abstractions.Security;

public interface ICurrentUser
{
    Guid Id { get; }
}
