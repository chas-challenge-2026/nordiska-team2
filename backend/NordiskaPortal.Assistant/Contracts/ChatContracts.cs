namespace NordiskaPortal.Assistant.Contracts
{
    // What the frontend sends and gets back. Deliberately tiny: one question
    // in, one answer out. No customer data in either direction.
    public record ChatRequest(string? Question);

    public record ChatResponse(string Answer);
}