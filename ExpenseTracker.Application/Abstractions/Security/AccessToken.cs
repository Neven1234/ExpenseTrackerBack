namespace ExpenseTracker.Application.Abstractions.Security;

public record AccessToken(string Value, DateTime ExpiresAtUtc);
