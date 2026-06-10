using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aktavara.WorkflowIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssistantContextController : ControllerBase
{
    private readonly IActivityLogParser _parser;
    private readonly ILogger<AssistantContextController> _logger;

    public AssistantContextController(
        IActivityLogParser parser,
        ILogger<AssistantContextController> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    /// <summary>
    /// Parses activity log content into raw entries.
    /// This is the first step before enrichment and workflow matching.
    /// </summary>
    [HttpPost("parse")]
    public ActionResult<IReadOnlyList<RawActivityLogEntry>> ParseActivityLog([FromBody] ParseActivityLogRequest request)
    {
        _logger.LogInformation("Parsing activity log for assistant context");

        try
        {
            var entries = _parser.Parse(request.Content);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing activity log");
            return BadRequest(new { error = ex.Message });
        }
    }
}

