using DoConnect.Api.Dtos;
using DoConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoConnect.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AiController : ControllerBase
{
    private readonly OpenAiService _openai;
    public AiController(OpenAiService openai) => _openai = openai;

    [HttpPost("chat")]
    [Authorize] // logged-in users only
    public async Task<ActionResult<AiChatResponse>> Chat([FromBody] AiChatRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest("Prompt is required.");

        var answer = await _openai.ChatAsync(req.Prompt, req.Model, ct);
        return Ok(new AiChatResponse { Answer = answer });
    }
}
