using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aktavara.WorkflowIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityLogController : ControllerBase
{
    private readonly IActivityLogParser _parser;
    private readonly ILogger<ActivityLogController> _logger;

    public ActivityLogController(IActivityLogParser parser, ILogger<ActivityLogController> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    /// <summary>
    /// Parses activity log content and returns raw activity log entries.
    /// </summary>
    [HttpPost("parse")]
    public ActionResult<IReadOnlyList<RawActivityLogEntry>> Parse([FromBody] ParseActivityLogRequest request)
    {
        _logger.LogInformation("Parsing activity log");

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

public class ParseActivityLogRequest
{
    public string Content { get; set; } = string.Empty;
}
