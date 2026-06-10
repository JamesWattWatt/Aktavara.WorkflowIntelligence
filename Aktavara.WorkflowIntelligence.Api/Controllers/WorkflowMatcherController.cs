using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aktavara.WorkflowIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowMatcherController : ControllerBase
{
    private readonly IWorkflowMatcher _matcher;
    private readonly IWorkflowLibrary _library;
    private readonly ILogger<WorkflowMatcherController> _logger;

    public WorkflowMatcherController(
        IWorkflowMatcher matcher,
        IWorkflowLibrary library,
        ILogger<WorkflowMatcherController> logger)
    {
        _matcher = matcher;
        _library = library;
        _logger = logger;
    }

    [HttpPost("match")]
    public ActionResult<IReadOnlyList<WorkflowMatchResult>> MatchWorkflows([FromBody] ActivityContext context)
    {
        _logger.LogInformation("Matching workflows for {Count} activities", context.RecentEvents.Count);

        try
        {
            var workflows = _library.GetAll();
            var matches = _matcher.FindMatches(context, workflows);
            return Ok(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching workflows");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("best-match")]
    public ActionResult<WorkflowMatchResult?> FindBestMatch([FromBody] ActivityContext context)
    {
        _logger.LogInformation("Finding best workflow match for {UserName}", context.UserName);

        try
        {
            var workflows = _library.GetAll();
            var match = _matcher.FindBestMatch(context, workflows, minimumConfidence: 0.55);

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
