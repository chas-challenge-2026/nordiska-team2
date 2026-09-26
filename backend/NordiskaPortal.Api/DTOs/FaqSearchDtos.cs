namespace NordiskaPortal.Api.DTOs;

public record FaqSearchRequest(string Query);

public record FaqSearchResponse(bool Matched, string? Question, string Answer, string? Category);
