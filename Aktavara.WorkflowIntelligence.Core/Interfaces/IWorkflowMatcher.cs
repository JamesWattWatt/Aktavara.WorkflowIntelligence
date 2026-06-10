namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

using Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Matches user activity against workflow definitions and scores the match confidence.
/// </summary>
public interface IWorkflowMatcher
{
    /// <summary>
    /// Finds matching workflows for the given activity context.
    /// Returns workflows ranked by confidence score (highest first).
    /// </summary>
    /// <param name="activityContext">The current user activity context to match against workflows.</param>
    /// <param name="workflows">The workflow definitions to match against.</param>
    /// <returns>Ranked list of workflow matches, ordered by confidence descending.</returns>
    IReadOnlyList<WorkflowMatchResult> FindMatches(
        ActivityContext activityContext,
        IReadOnlyList<WorkflowDefinition> workflows);

    /// <summary>
    /// Finds the top matching workflow for the given activity context.
    /// </summary>
    /// <param name="activityContext">The current user activity context.</param>
    /// <param name="workflows">The workflow definitions to match against.</param>
    /// <param name="minimumConfidence">Minimum confidence threshold (0-1). Workflows below this are excluded.</param>
    /// <returns>The best matching workflow, or null if no matches meet the threshold.</returns>
    WorkflowMatchResult? FindBestMatch(
        ActivityContext activityContext,
        IReadOnlyList<WorkflowDefinition> workflows,
        double minimumConfidence = 0.55);

    /// <summary>
    /// Scores a single workflow against the given activity context.
    /// </summary>
    /// <param name="workflow">The workflow to score.</param>
    /// <param name="activityContext">The activity context to match against.</param>
    /// <returns>The match result with confidence score and breakdown.</returns>
    WorkflowMatchResult ScoreWorkflow(
        WorkflowDefinition workflow,
        ActivityContext activityContext);
}
