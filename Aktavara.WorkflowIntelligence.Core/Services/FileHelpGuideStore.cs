using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Loads help guides from markdown files in help-guides/ folder.
/// Parses sections and maps them to workflow steps.
/// </summary>
public class FileHelpGuideStore : IHelpGuideStore
{
    private readonly string _helpGuidesPath;
    private readonly ILogger<FileHelpGuideStore> _logger;
    private Dictionary<string, HelpGuide>? _cache;
    private Dictionary<string, List<HelpGuideSection>>? _stepMappings;
    private bool _loaded;

    public FileHelpGuideStore(string helpGuidesPath, ILogger<FileHelpGuideStore> logger)
    {
        _helpGuidesPath = helpGuidesPath;
        _logger = logger;
        _loaded = false;
    }

    public HelpGuide? GetById(string helpGuideId)
    {
        EnsureLoaded();
        return _cache?.GetValueOrDefault(helpGuideId);
    }

    public HelpGuide? GetByFileName(string fileName)
    {
        EnsureLoaded();
        var id = Path.GetFileNameWithoutExtension(fileName);
        return _cache?.GetValueOrDefault(id);
    }

    public HelpGuideSection? GetSection(string fileName, string sectionId)
    {
        var guide = GetByFileName(fileName);
        if (guide == null)
            return null;

        return guide.Sections.FirstOrDefault(s => s.SectionId == sectionId);
    }

    public IReadOnlyList<HelpGuideSection> GetByWorkflowAndStep(string workflowId, string stepId)
    {
        EnsureLoaded();
        var normalizedStepId = NormalizeStepId(stepId);
        var key = $"{workflowId}:{normalizedStepId}";
        return _stepMappings?.GetValueOrDefault(key) ?? new List<HelpGuideSection>();
    }

    public IReadOnlyList<HelpGuide> GetAll()
    {
        EnsureLoaded();
        return _cache?.Values.ToList() ?? new List<HelpGuide>();
    }

    public IReadOnlyList<string> GetWorkspaceTypes()
    {
        EnsureLoaded();
        return _cache?.Values
            .Select(g => g.WorkspaceType)
            .Distinct()
            .OrderBy(x => x)
            .ToList() ?? new List<string>();
    }

    public void Reload()
    {
        _loaded = false;
        _cache = null;
        _stepMappings = null;
        EnsureLoaded();
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        _cache = new Dictionary<string, HelpGuide>();
        _stepMappings = new Dictionary<string, List<HelpGuideSection>>();
        _loaded = true;

        if (!Directory.Exists(_helpGuidesPath))
        {
            _logger.LogInformation("Help guides directory not found at {Path}", _helpGuidesPath);
            return;
        }

        // Load all markdown files
        var mdFiles = Directory.GetFiles(_helpGuidesPath, "*.md", SearchOption.TopDirectoryOnly)
            .Where(f => !Path.GetFileName(f).Equals("index.md", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();

        _logger.LogInformation("Found {Count} help guide markdown files", mdFiles.Count);

        foreach (var filePath in mdFiles)
        {
            try
            {
                var guide = ParseHelpGuideFile(filePath);
                if (guide != null)
                {
                    _cache[guide.HelpGuideId] = guide;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing help guide file {FilePath}", filePath);
            }
        }

        // Load workflow-guide-mapping.json
        var mappingPath = Path.Combine(_helpGuidesPath, "workflow-guide-mapping.json");
        if (File.Exists(mappingPath))
        {
            try
            {
                LoadWorkflowMappings(mappingPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading workflow-guide-mapping.json");
            }
        }

        _logger.LogInformation("Loaded {Count} help guides", _cache.Count);
    }

    private HelpGuide? ParseHelpGuideFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var helpGuideId = Path.GetFileNameWithoutExtension(fileName);
        var content = File.ReadAllText(filePath);

        var guide = new HelpGuide
        {
            HelpGuideId = helpGuideId,
            FileName = fileName,
            WorkspaceType = InferWorkspaceType(fileName),
            MarkdownContent = content,
            LastModified = File.GetLastWriteTimeUtc(filePath)
        };

        // Extract title from first # heading
        var titleMatch = Regex.Match(content, @"^#\s+(.+)$", RegexOptions.Multiline);
        guide.Title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : helpGuideId;

        // Parse sections
        guide.Sections = ParseSections(content);

        return guide;
    }

    private List<HelpGuideSection> ParseSections(string content)
    {
        var sections = new List<HelpGuideSection>();
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var currentSection = new StringBuilder();
        HelpGuideSection? currentParsedSection = null;
        int? currentLevel = null;
        string? lastLevel2SectionId = null;
        var sectionIdCounts = new Dictionary<string, int>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var headingMatch = Regex.Match(line, @"^(#{2,3})\s+(.+)$");

            if (headingMatch.Success)
            {
                // Save the previous section
                if (currentParsedSection != null)
                {
                    currentParsedSection.Content = currentSection.ToString().TrimEnd();
                    sections.Add(currentParsedSection);
                }

                // Start a new section
                var level = headingMatch.Groups[1].Value.Length;
                var heading = headingMatch.Groups[2].Value.Trim();
                var sectionId = SlugifyHeading(heading);

                // Handle duplicate section IDs
                if (sectionIdCounts.ContainsKey(sectionId))
                {
                    sectionIdCounts[sectionId]++;
                    sectionId = $"{sectionId}-{sectionIdCounts[sectionId]}";
                }
                else
                {
                    sectionIdCounts[sectionId] = 1;
                }

                string? parentSectionId = null;
                if (level == 3 && lastLevel2SectionId != null)
                {
                    parentSectionId = lastLevel2SectionId;
                }

                currentParsedSection = new HelpGuideSection
                {
                    SectionId = sectionId,
                    Heading = heading,
                    Level = level,
                    ParentSectionId = parentSectionId
                };

                if (level == 2)
                {
                    lastLevel2SectionId = sectionId;
                }

                currentLevel = level;
                currentSection = new StringBuilder();
            }
            else
            {
                currentSection.AppendLine(line);
            }
        }

        // Save the last section
        if (currentParsedSection != null)
        {
            currentParsedSection.Content = currentSection.ToString().TrimEnd();
            sections.Add(currentParsedSection);
        }

        return sections;
    }

    private string NormalizeStepId(string stepId)
    {
        return stepId
            .ToLowerInvariant()
            .Trim()
            .Replace(" ", "_")
            .Replace("-", "_");
    }

    private string SlugifyHeading(string heading)
    {
        var slug = heading.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^\w\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }

    private string InferWorkspaceType(string fileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        if (nameWithoutExt.EndsWith("_Workspace", StringComparison.OrdinalIgnoreCase))
        {
            var type = nameWithoutExt[..^"_Workspace".Length];
            return type;
        }

        return "General";
    }

    private void LoadWorkflowMappings(string mappingPath)
    {
        var json = File.ReadAllText(mappingPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("mappings", out var mappingsElement))
            return;

        foreach (var mapping in mappingsElement.EnumerateArray())
        {
            try
            {
                var workflowId = mapping.GetProperty("workflowId").GetString() ?? "";
                var stepId = mapping.GetProperty("stepId").GetString() ?? "";
                var guideFile = mapping.GetProperty("guideFile").GetString() ?? "";
                string? sectionId = null;

                if (mapping.TryGetProperty("sectionId", out var sectionIdElement) &&
                    sectionIdElement.ValueKind != JsonValueKind.Null)
                {
                    sectionId = sectionIdElement.GetString();
                }

                var guide = GetByFileName(guideFile);
                if (guide == null)
                {
                    _logger.LogWarning("Workflow mapping references missing guide file: {GuideFile}", guideFile);
                    continue;
                }

                HelpGuideSection? section = null;

                if (sectionId != null)
                {
                    section = guide.Sections.FirstOrDefault(s => s.SectionId == sectionId);
                    if (section == null)
                    {
                        _logger.LogWarning("Workflow mapping references missing section {SectionId} in {GuideFile}", sectionId, guideFile);
                        // Use guide intro (first section or all content before first ##)
                        section = new HelpGuideSection
                        {
                            SectionId = "intro",
                            Heading = guide.Title,
                            Level = 2,
                            Content = ExtractGuideIntro(guide.MarkdownContent)
                        };
                    }
                }
                else
                {
                    // Use guide intro
                    section = new HelpGuideSection
                    {
                        SectionId = "intro",
                        Heading = guide.Title,
                        Level = 2,
                        Content = ExtractGuideIntro(guide.MarkdownContent)
                    };
                }

                section.RelevantStepIds.Add(stepId);

                var normalizedStepId = NormalizeStepId(stepId);
                var key = $"{workflowId}:{normalizedStepId}";
                if (!_stepMappings!.ContainsKey(key))
                {
                    _stepMappings[key] = new List<HelpGuideSection>();
                }
                _stepMappings[key].Add(section);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing workflow mapping");
            }
        }
    }

    private string ExtractGuideIntro(string content)
    {
        // Content before the first ## heading
        var firstHeadingIndex = content.IndexOf("\n##", StringComparison.OrdinalIgnoreCase);
        if (firstHeadingIndex < 0)
            return content;

        return content[..firstHeadingIndex].Trim();
    }
}
