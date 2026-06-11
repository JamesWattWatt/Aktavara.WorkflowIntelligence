namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Indicates the level of guidance to provide to the user based on workflow confidence.
/// </summary>
public enum GuidanceLevel
{
    /// <summary>
    /// No workflow matches above minimum threshold - cannot provide guidance.
    /// </summary>
    NoGuidance = 0,

    /// <summary>
    /// Low confidence match - offer a suggestion but acknowledge uncertainty.
    /// </summary>
    Suggest = 1,

    /// <summary>
    /// Medium confidence match - ask user to confirm the detected workflow.
    /// </summary>
    Confirm = 2,

    /// <summary>
    /// High confidence match - provide direct next step instruction.
    /// </summary>
    Instruct = 3
}
