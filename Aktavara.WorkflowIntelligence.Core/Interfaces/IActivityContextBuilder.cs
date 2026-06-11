using Aktavara.WorkflowIntelligence.Core.Models;

namespace Aktavara.WorkflowIntelligence.Core.Interfaces;

/// <summary>
/// Builds a comprehensive activity context from normalized activity events.
/// The context includes the current state, active entities, workflow hints,
/// and a human-readable summary of the user's recent activity.
/// </summary>
public interface IActivityContextBuilder
{
    /// <summary>
    /// Builds an activity context from the given events for a specific user and time window.
    /// </summary>
    /// <param name="allEvents">All normalized activity events.</param>
    /// <param name="userName">The user whose context is being built.</param>
    /// <param name="timeWindowStart">Start of the time window.</param>
    /// <param name="timeWindowEnd">End of the time window.</param>
    /// <returns>A complete ActivityContext with state, entities, hints, and summary.</returns>
    ActivityContext BuildContext(
        IReadOnlyList<ActivityEvent> allEvents,
        string userName,
        DateTime timeWindowStart,
        DateTime timeWindowEnd);
}
