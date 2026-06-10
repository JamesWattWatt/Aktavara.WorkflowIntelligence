using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aktavara.WorkflowIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowMatcherController : ControllerBase
{
    private readonly IWorkflowMatcher _matcher;
    private readonly ILogger<WorkflowMatcherController> _logger;

    public WorkflowMatcherController(IWorkflowMatcher matcher, ILogger<WorkflowMatcherController> logger)
    {
        _matcher = matcher;
        _logger = logger;
    }

    [HttpPost("match")]
    public async Task<ActionResult<List<WorkflowMatch>>> MatchWorkflows([FromBody] List<ActivityLogEntry> activities)
    {
        _logger.LogInformation("Matching workflows for {Count} activities", activities.Count);

        try
        {
            var matches = await _matcher.MatchWorkflowsAsync(activities);
            return Ok(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching workflows");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("best-match")]
    public async Task<ActionResult<WorkflowMatch?>> FindBestMatch([FromBody] List<ActivityLogEntry> activities)
    {
        _logger.LogInformation("Finding best workflow match");

        try
        {
            var match = await _matcher.FindBestMatchAsync(activities);
            if (match == null)
            {
                return NotFound("No matching workflow found");
            }

            return Ok(match);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding best match");
            return BadRequest(new { error = ex.Message });
        }
    }
}
