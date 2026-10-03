namespace AuthPractice.Contracts;

// Ответ тестового endpoint'а /api/ping
public record PingResponse(string Message, DateTime ServerTimeUtc);
