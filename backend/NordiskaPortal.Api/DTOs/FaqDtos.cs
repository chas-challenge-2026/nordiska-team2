namespace NordiskaPortal.Api.DTOs;

public record FaqSearchRequest(string Query);

public record FaqSearchResponse(bool Matched, string? Question, string Answer, string? Category);

public record FaqCategoryResponse(string Id, string Label, int QuestionCount);

public record FaqEntryResponse(int Id, string Question, string Answer, string Category);