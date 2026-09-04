namespace ExpenseTracker.Application.DTOs.Auth;

public record AuthResponse(Guid UserId, string Email, string Username, string Token, DateTime ExpiresAtUtc);
