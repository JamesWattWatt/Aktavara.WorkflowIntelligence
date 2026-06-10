using Aktavara.WorkflowIntelligence.Core.Models;
using Xunit;

namespace Aktavara.WorkflowIntelligence.Tests;

public class WorkflowDefinitionTests
{
    [Fact]
    public void Validate_WithAllRequiredFields_ReturnsEmpty()
    {
        var workflow = CreateValidWorkflow();
        var errors = workflow.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithEmptyWorkflowId_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.WorkflowId = string.Empty;

        var errors = workflow.Validate();
        Assert.Single(errors);
        Assert.Contains("WorkflowId is required", errors[0]);
    }

    [Fact]
    public void Validate_WithNullWorkflowId_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.WorkflowId = null;

        var errors = workflow.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains("WorkflowId is required", errors[0]);
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.Name = string.Empty;

        var errors = workflow.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains("Name is required", errors[0]);
    }

    [Fact]
    public void Validate_WithNoSignatureRules_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.ActivitySignature.Clear();

        var errors = workflow.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains("At least one activity signature rule is required", errors[0]);
    }

    [Fact]
    public void Validate_WithNoRequiredRules_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        foreach (var rule in workflow.ActivitySignature)
        {
            rule.Required = false;
        }

        var errors = workflow.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains("At least one activity signature rule must be marked as required", errors[0]);
    }

    [Fact]
    public void Validate_WithConfidenceTooLow_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.MinimumConfidenceThreshold = -0.1;

        var errors = workflow.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains("MinimumConfidenceThreshold must be between 0.0 and 1.0", errors[0]);
    }

    [Fact]
    public void Validate_WithConfidenceTooHigh_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.MinimumConfidenceThreshold = 1.5;

        var errors = workflow.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains("MinimumConfidenceThreshold must be between 0.0 and 1.0", errors[0]);
    }

    [Fact]
    public void Validate_WithConfidenceZero_IsValid()
    {
        var workflow = CreateValidWorkflow();
        workflow.MinimumConfidenceThreshold = 0.0;

        var errors = workflow.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithConfidenceOne_IsValid()
    {
        var workflow = CreateValidWorkflow();
        workflow.MinimumConfidenceThreshold = 1.0;

        var errors = workflow.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithDuplicateStateIds_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        var duplicateState = new WorkflowStateDefinition
        {
            StateId = "initial",
            Name = "Duplicate",
            Sequence = 1
        };
        workflow.States.Add(duplicateState);

        var errors = workflow.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains("Duplicate state ID", errors[0]);
    }

    [Fact]
    public void Validate_WithInvalidNextStateId_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.States[0].NextStateId = "nonexistent-state";

        var errors = workflow.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains("references non-existent next state", errors[0]);
    }

    [Fact]
    public void Validate_WithValidStateTransitions_ReturnsEmpty()
    {
        var workflow = new WorkflowDefinition
        {
            WorkflowId = "test",
            Name = "Test",
            ActivitySignature = new()
            {
                new WorkflowSignatureRule
                {
                    EventType = EventType.SearchRecords,
                    Required = true,
                    Weight = 1.0
                }
            },
            States = new()
            {
                new WorkflowStateDefinition
                {
                    StateId = "state1",
                    Name = "State 1",
                    Sequence = 0,
                    NextStateId = "state2",
                    RequiredEvidence = new() { "event1" }
                },
                new WorkflowStateDefinition
                {
                    StateId = "state2",
                    Name = "State 2",
                    Sequence = 1,
                    IsTerminal = true,
                    RequiredEvidence = new() { "event2" }
                }
            }
        };

        var errors = workflow.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithWeightsTooBig_ReturnsWarning()
    {
        var workflow = CreateValidWorkflow();
        workflow.ActivitySignature.Clear();
        // Add rules with very high weights
        for (int i = 0; i < 3; i++)
        {
            workflow.ActivitySignature.Add(new WorkflowSignatureRule
            {
                EventType = EventType.SearchRecords,
                Required = true,
                Weight = 5.0
            });
        }

        var errors = workflow.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains("Total weight", errors[0]);
    }

    [Fact]
    public void Validate_WithWeightsTooSmall_ReturnsWarning()
    {
        var workflow = CreateValidWorkflow();
        foreach (var rule in workflow.ActivitySignature)
        {
            rule.Weight = 0.01;
        }

        var errors = workflow.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains("Total weight", errors[0]);
    }

    [Fact]
    public void IsValid_WithValidWorkflow_ReturnsTrue()
    {
        var workflow = CreateValidWorkflow();
        Assert.True(workflow.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidWorkflow_ReturnsFalse()
    {
        var workflow = CreateValidWorkflow();
        workflow.ActivitySignature.Clear();
        Assert.False(workflow.IsValid());
    }

    [Fact]
    public void IsValid_WithMultipleErrors_ReturnsFalse()
    {
        var workflow = new WorkflowDefinition
        {
            WorkflowId = string.Empty,
            Name = string.Empty,
            ActivitySignature = new()
        };
        Assert.False(workflow.IsValid());
    }

    [Fact]
    public void GetInitialState_WithStates_ReturnsLowestSequence()
    {
        var workflow = new WorkflowDefinition
        {
            WorkflowId = "test",
            Name = "Test",
            ActivitySignature = new()
            {
                new WorkflowSignatureRule { EventType = EventType.SearchRecords, Required = true }
            },
            States = new()
            {
                new WorkflowStateDefinition { StateId = "state2", Sequence = 2 },
                new WorkflowStateDefinition { StateId = "state0", Sequence = 0 },
                new WorkflowStateDefinition { StateId = "state1", Sequence = 1 }
            }
        };

        var initial = workflow.GetInitialState();
        Assert.NotNull(initial);
        Assert.Equal("state0", initial.StateId);
    }

    [Fact]
    public void GetStateById_WithExistingState_ReturnsState()
    {
        var workflow = CreateValidWorkflow();
        var state = workflow.GetStateById("initial");
        Assert.NotNull(state);
        Assert.Equal("initial", state.StateId);
    }

    [Fact]
    public void GetStateById_WithNonExistentState_ReturnsNull()
    {
        var workflow = CreateValidWorkflow();
        var state = workflow.GetStateById("nonexistent");
        Assert.Null(state);
    }

    [Fact]
    public void GetRequiredSignatures_ReturnsOnlyRequired()
    {
        var workflow = CreateValidWorkflow();
        var required = new WorkflowSignatureRule
        {
            EventType = EventType.OpenWorkspace,
            Required = true,
            Weight = 1.0
        };
        var optional = new WorkflowSignatureRule
        {
            EventType = EventType.SaveRecords,
            Required = false,
            Weight = 0.5
        };

        workflow.ActivitySignature.Add(required);
        workflow.ActivitySignature.Add(optional);

        var requiredRules = workflow.GetRequiredSignatures();
        Assert.Equal(2, requiredRules.Count); // Initial rule + required
        Assert.All(requiredRules, r => Assert.True(r.Required));
    }

    [Fact]
    public void MatchesSignature_WithAllRequiredEventsPresent_ReturnsTrue()
    {
        var workflow = CreateValidWorkflow();
        var events = new List<ActivityEvent>
        {
            new ActivityEvent
            {
                EventType = EventType.SearchRecords,
                RecordKind = RecordKind.Path,
                Timestamp = DateTime.UtcNow
            }
        };

        var matches = workflow.MatchesSignature(events);
        Assert.True(matches);
    }

    [Fact]
    public void MatchesSignature_WithMissingRequiredEvent_ReturnsFalse()
    {
        var workflow = new WorkflowDefinition
        {
            WorkflowId = "test",
            Name = "Test",
            ActivitySignature = new()
            {
                new WorkflowSignatureRule
                {
                    EventType = EventType.SearchRecords,
                    RecordKind = RecordKind.Path,
                    Required = true
                }
            },
            States = new()
            {
                new WorkflowStateDefinition { StateId = "initial", Sequence = 0 }
            }
        };

        var events = new List<ActivityEvent>
        {
            new ActivityEvent
            {
                EventType = EventType.OpenWorkspace,
                RecordKind = RecordKind.Path,
                Timestamp = DateTime.UtcNow
            }
        };

        var matches = workflow.MatchesSignature(events);
        Assert.False(matches);
    }

    [Fact]
    public void IsActive_WithActiveStatus_ReturnsTrue()
    {
        var workflow = CreateValidWorkflow();
        workflow.Status = WorkflowStatus.Active;
        Assert.True(workflow.IsActive);
    }

    [Fact]
    public void IsActive_WithInactiveStatus_ReturnsFalse()
    {
        var workflow = CreateValidWorkflow();
        workflow.Status = WorkflowStatus.Inactive;
        Assert.False(workflow.IsActive);
    }

    #region Helper Methods

    private WorkflowDefinition CreateValidWorkflow()
    {
        return new WorkflowDefinition
        {
            WorkflowId = "test-workflow",
            Name = "Test Workflow",
            Description = "A test workflow",
            Version = "1.0",
            Status = WorkflowStatus.Active,
            ActivitySignature = new()
            {
                new WorkflowSignatureRule
                {
                    EventType = EventType.SearchRecords,
                    RecordKind = RecordKind.Path,
                    Required = true,
                    Weight = 1.0
                }
            },
            States = new()
            {
                new WorkflowStateDefinition
                {
                    StateId = "initial",
                    Name = "Initial State",
                    Description = "Starting state",
                    Sequence = 0,
                    IsTerminal = false,
                    RequiredEvidence = new() { "SearchRecords" }
                }
            },
            MinimumConfidenceThreshold = 0.6
        };
    }

    #endregion
}
