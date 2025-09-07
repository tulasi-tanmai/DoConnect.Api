namespace DoConnect.Api.Dtos;

public sealed class AiChatRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? Model { get; set; }  // optional (default below)
}

public sealed class AiChatResponse
{
    public string Answer { get; set; } = string.Empty;
}
