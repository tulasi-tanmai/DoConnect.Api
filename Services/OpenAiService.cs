using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DoConnect.Api.Services;

public sealed class OpenAiService
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<OpenAiService> _log;
    private readonly string _apiKey;
    private static readonly JsonSerializerOptions J = new(JsonSerializerDefaults.Web);

    public OpenAiService(IHttpClientFactory http, ILogger<OpenAiService> log, IConfiguration cfg)
    {
        _http = http; _log = log;
        _apiKey = cfg["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        if (string.IsNullOrWhiteSpace(_apiKey))
            _log.LogWarning("OpenAI API key missing. Set OPENAI_API_KEY or OpenAI:ApiKey.");
    }

    public async Task<string> ChatAsync(string prompt, string? model, CancellationToken ct = default)
    {
        var client = _http.CreateClient("openai");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        // Chat Completions (OpenAI, text-only answer)
        var body = new
        {
            model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model, // cost-effective default
            messages = new object[]
            {
                new { role = "system", content = "You are a helpful assistant for a Q&A site called DoConnect." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        var res = await client.PostAsync(
            "v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(body, J), Encoding.UTF8, "application/json"),
            ct);

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(ct);
            _log.LogWarning("OpenAI error {Code}: {Body}", (int)res.StatusCode, err);
            throw new InvalidOperationException("AI backend call failed.");
        }

        using var s = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);
        var answer = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

        return answer.Trim();
    }
}
