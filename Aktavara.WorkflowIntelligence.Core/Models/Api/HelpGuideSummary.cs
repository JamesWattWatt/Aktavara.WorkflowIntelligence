namespace Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// Summary information about a help guide for list endpoints.
/// </summary>
public class HelpGuideSummary
{
    public string HelpGuideId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string WorkspaceType { get; set; } = string.Empty;
    public int SectionCount { get; set; }
    public DateTime LastModified { get; set; }
}
