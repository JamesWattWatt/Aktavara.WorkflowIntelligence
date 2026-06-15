using System.Text.Json;
using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Loads workflow definitions from *.workflow.json files in a configured directory.
/// Provides indexed access to workflows and validation of workflow definitions.
/// </summary>
public class FileWorkflowLibrary : IWorkflowLibrary
{
    private readonly string _workflowDirectory;
    private readonly ILogger<FileWorkflowLibrary> _logger;
    private Dictionary<string, WorkflowDefinition> _workflows = new();
    private Dictionary<string, List<string>> _validationErrors = new();
    private bool _isLoaded;

    public FileWorkflowLibrary(string workflowDirectory, ILogger<FileWorkflowLibrary> logger)
    {
        _workflowDirectory = workflowDirectory;
        _logger = logger;
    }

    /// <summary>
    /// Loads all workflows from the configured directory.
    /// This is called automatically on first access but can be called manually to reload.
    /// </summary>
    public async Task LoadAsync()
    {
        _workflows = new();
        _validationErrors = new();

        if (!Directory.Exists(_workflowDirectory))
        {
            _logger.LogWarning("Workflow directory does not exist: {Directory}", _workflowDirectory);
            _isLoaded = true;
            return;
        }

        var workflowFiles = Directory.GetFiles(_workflowDirectory, "*.workflow.json", SearchOption.TopDirectoryOnly);

        _logger.LogInformation("Loading {Count} workflow files from {Directory}", workflowFiles.Length, _workflowDirectory);

        foreach (var filePath in workflowFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var jsonPreview = json.Length > 200 ? json.Substring(0, 200) : json;
                _logger.LogInformation(
                    "Read workflow file {FilePath}: {Length} bytes. Preview: {Preview}",
                    filePath,
                    json.Length,
                    jsonPreview);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = false
                };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(json, options);

                if (workflow == null)
                {
                    _logger.LogError("Failed to deserialize workflow from {FilePath}: result is null", filePath);
                    continue;
                }

                // Validate the workflow
                var errors = workflow.Validate();
                _logger.LogInformation(
                    "Workflow parsed: WorkflowId={WorkflowId}, Name={Name}, Rules={RuleCount}, Validation Errors={ErrorCount}",
                    workflow.WorkflowId ?? "<null>",
                    workflow.Name ?? "<null>",
                    workflow.ActivitySignature?.Count ?? 0,
                    errors.Count);

                if (errors.Count > 0)
                {
                    _logger.LogError(
                        "Workflow {WorkflowId} has validation errors: {Errors}",
                        workflow.WorkflowId,
                        string.Join("; ", errors));
                    _validationErrors[workflow.WorkflowId] = errors;
                    continue;
                }

                if (_workflows.ContainsKey(workflow.WorkflowId))
                {
                    _logger.LogWarning(
                        "Duplicate workflow ID {WorkflowId}. Using definition from {FilePath}",
                        workflow.WorkflowId,
                        filePath);
                }

                _workflows[workflow.WorkflowId] = workflow;
                _logger.LogInformation("Loaded workflow: {WorkflowId}", workflow.WorkflowId);
            }
            catch (JsonException ex)
            {
                var innerMessage = ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "";
                _logger.LogError(
                    "Invalid JSON in workflow file {FilePath}: {Message}{InnerMessage}",
                    filePath,
                    ex.Message,
                    innerMessage);
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "";
                _logger.LogError(
                    "Error loading workflow file {FilePath}: {Type}: {Message}{InnerMessage}",
                    filePath,
                    ex.GetType().Name,
                    ex.Message,
                    innerMessage);
            }
        }

        _isLoaded = true;
        _logger.LogInformation("Workflow library loaded: {Count} valid workflows", _workflows.Count);
    }

    /// <summary>
    /// Ensures workflows are loaded before access.
    /// </summary>
    private async Task EnsureLoadedAsync()
    {
        if (!_isLoaded)
        {
            await LoadAsync();
        }
    }

    /// <summary>
    /// Gets all workflow definitions in the library.
    /// </summary>
    public IReadOnlyList<WorkflowDefinition> GetAll()
    {
        // This method should be called after LoadAsync, but we'll ensure loading for convenience
        // In production, you might want to make this async or require explicit loading
        EnsureLoadedAsync().Wait();
        return _workflows.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets a specific workflow definition by its unique identifier.
    /// </summary>
    public WorkflowDefinition? GetById(string workflowId)
    {
        EnsureLoadedAsync().Wait();

        if (string.IsNullOrWhiteSpace(workflowId))
            return null;

        _workflows.TryGetValue(workflowId, out var workflow);
        return workflow;
    }

    /// <summary>
    /// Gets all workflows that match a given tag.
    /// </summary>
    public IReadOnlyList<WorkflowDefinition> GetByTag(string tag)
    {
        EnsureLoadedAsync().Wait();

        if (string.IsNullOrWhiteSpace(tag))
            return new List<WorkflowDefinition>().AsReadOnly();

        return _workflows.Values
            .Where(w => w.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets all active/enabled workflows.
    /// </summary>
    public IReadOnlyList<WorkflowDefinition> GetActive()
    {
        EnsureLoadedAsync().Wait();
        return _workflows.Values
            .Where(w => w.IsActive)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets validation errors for a workflow (if any).
    /// Returns empty list if the workflow is valid.
    /// </summary>
    public IReadOnlyList<string> GetValidationErrors(string workflowId)
    {
        EnsureLoadedAsync().Wait();

        if (_validationErrors.TryGetValue(workflowId, out var errors))
            return errors.AsReadOnly();

        return new List<string>().AsReadOnly();
    }

    /// <summary>
    /// Saves or updates a workflow definition to the library.
    /// </summary>
    public async Task<bool> SaveWorkflowAsync(WorkflowDefinition workflow)
    {
        try
        {
            await EnsureLoadedAsync();

            if (string.IsNullOrWhiteSpace(workflow.WorkflowId))
            {
                _logger.LogError("Cannot save workflow with null or empty WorkflowId");
                return false;
            }

            // Validate the workflow
            var errors = workflow.Validate();
            if (errors.Count > 0)
            {
                _logger.LogError("Cannot save invalid workflow {WorkflowId}: {Errors}",
                    workflow.WorkflowId, string.Join("; ", errors));
                _validationErrors[workflow.WorkflowId] = errors;
                return false;
            }

            // Ensure directory exists
            if (!Directory.Exists(_workflowDirectory))
            {
                Directory.CreateDirectory(_workflowDirectory);
            }

            // Write to file
            var fileName = $"{workflow.WorkflowId}.workflow.json";
            var filePath = Path.Combine(_workflowDirectory, fileName);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var json = JsonSerializer.Serialize(workflow, options);
            await File.WriteAllTextAsync(filePath, json);

            // Update in-memory cache
            _workflows[workflow.WorkflowId] = workflow;
            _validationErrors.Remove(workflow.WorkflowId);

            _logger.LogInformation("Saved workflow: {WorkflowId} to {FilePath}", workflow.WorkflowId, filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving workflow {WorkflowId}", workflow?.WorkflowId ?? "<unknown>");
            return false;
        }
    }

    /// <summary>
    /// Deletes a workflow from the library.
    /// </summary>
    public async Task<bool> DeleteWorkflowAsync(string workflowId)
    {
        try
        {
            await EnsureLoadedAsync();

            if (string.IsNullOrWhiteSpace(workflowId))
            {
                _logger.LogError("Cannot delete workflow with null or empty ID");
                return false;
            }

            var fileName = $"{workflowId}.workflow.json";
            var filePath = Path.Combine(_workflowDirectory, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted workflow file: {FilePath}", filePath);
            }

            // Remove from in-memory cache
            _workflows.Remove(workflowId);
            _validationErrors.Remove(workflowId);

            _logger.LogInformation("Deleted workflow: {WorkflowId}", workflowId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow {WorkflowId}", workflowId);
            return false;
        }
    }
}
