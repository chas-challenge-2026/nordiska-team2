namespace NordiskaPortal.Api.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken);