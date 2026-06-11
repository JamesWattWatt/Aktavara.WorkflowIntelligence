namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// A help guide chapter from the Aktavara documentation.
/// Represents a markdown file with extracted sections.
/// </summary>
public class HelpGuide
{
    /// <summary>
    /// Unique identifier (filename without extension, e.g. "Path_Workspace")
    /// </summary>
    public string HelpGuideId { get; set; } = string.Empty;

    /// <summary>
    /// Title extracted from first # heading in the file
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Original filename (e.g. "Path_Workspace.md")
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Inferred from filename: "Path", "Topology", "Connections", or "General"
    /// </summary>
    public string WorkspaceType { get; set; } = string.Empty;

    /// <summary>
    /// Full markdown content of the file
    /// </summary>
    public string MarkdownContent { get; set; } = string.Empty;

    /// <summary>
    /// Parsed sections from the markdown file
    /// </summary>
    public List<HelpGuideSection> Sections { get; set; } = new();

    /// <summary>
    /// When the file was last modified
    /// </summary>
    public DateTime LastModified { get; set; }
}

/// <summary>
/// A section within a help guide, extracted from ## or ### headings
/// </summary>
public class HelpGuideSection
{
    /// <summary>
    /// Slugified section ID (lowercase, spaces to hyphens)
    /// </summary>
    public string SectionId { get; set; } = string.Empty;

    /// <summary>
    /// Exact heading text from the file
    /// </summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>
    /// Heading level: 2 for ##, 3 for ###
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Markdown content from this heading to the next same-or-higher level
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Parent section ID if this is a ### under a ##
    /// </summary>
    public string? ParentSectionId { get; set; }

    /// <summary>
    /// Workflow step IDs that this section applies to
    /// </summary>
    public List<string> RelevantStepIds { get; set; } = new();
}
