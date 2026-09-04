namespace ExpenseTracker.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string resource) : base($"{resource} was not found.")
    {
    }
}
