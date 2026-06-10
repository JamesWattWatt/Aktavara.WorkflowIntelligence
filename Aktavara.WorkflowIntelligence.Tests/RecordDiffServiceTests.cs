using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aktavara.WorkflowIntelligence.Tests;

public class RecordDiffServiceTests
{
    private readonly RecordDiffService _diffService;

    public RecordDiffServiceTests()
    {
        var mockLogger = new Mock<ILogger<RecordDiffService>>();
        _diffService = new RecordDiffService(mockLogger.Object);
    }

    #region Basic Diffing Tests

    [Fact]
    public void Diff_IdenticalRecords_ReturnsEmptyList()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test Node", ValueType = "String" },
                new() { AttributeId = "Description", Value = "A test", ValueType = "String" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test Node", ValueType = "String" },
                new() { AttributeId = "Description", Value = "A test", ValueType = "String" }
            }
        };

        var result = _diffService.Diff(before, after);

        Assert.Empty(result);
    }

    [Fact]
    public void Diff_SingleAttributeChanged_ReturnsSingleChange()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "NE4", ValueType = "String" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "NE411", ValueType = "String" }
            }
        };

        var result = _diffService.Diff(before, after);

        Assert.Single(result);
        var change = result[0];
        Assert.Equal("Name", change.AttributeId);
        Assert.Equal("NE4", change.FromValue);
        Assert.Equal("NE411", change.ToValue);
        Assert.Equal("String", change.ValueType);
    }

    [Fact]
    public void Diff_MultipleAttributesChanged_ReturnsAllChanges()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Old Name", ValueType = "String" },
                new() { AttributeId = "Description", Value = "Old Description", ValueType = "String" },
                new() { AttributeId = "Count", Value = "10", ValueType = "Integer" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "New Name", ValueType = "String" },
                new() { AttributeId = "Description", Value = "Old Description", ValueType = "String" }, // No change
                new() { AttributeId = "Count", Value = "42", ValueType = "Integer" }
            }
        };

        var result = _diffService.Diff(before, after);

        Assert.Equal(2, result.Count);

        var nameChange = result.FirstOrDefault(c => c.AttributeId == "Name");
        Assert.NotNull(nameChange);
        Assert.Equal("Old Name", nameChange.FromValue);
        Assert.Equal("New Name", nameChange.ToValue);

        var countChange = result.FirstOrDefault(c => c.AttributeId == "Count");
        Assert.NotNull(countChange);
        Assert.Equal("10", countChange.FromValue);
        Assert.Equal("42", countChange.ToValue);
    }

    #endregion

    #region Missing/Added Attributes Tests

    [Fact]
    public void Diff_AttributeAddedInAfter_ReturnsChange()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" },
                new() { AttributeId = "NewAttribute", Value = "NewValue", ValueType = "String" }
            }
        };

        var result = _diffService.Diff(before, after);

        Assert.Single(result);
        var change = result[0];
        Assert.Equal("NewAttribute", change.AttributeId);
        Assert.Null(change.FromValue);
        Assert.Equal("NewValue", change.ToValue);
    }

    [Fact]
    public void Diff_AttributeRemovedInAfter_ReturnsChange()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" },
                new() { AttributeId = "RemoveMe", Value = "OldValue", ValueType = "String" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" }
            }
        };

        var result = _diffService.Diff(before, after);

        Assert.Single(result);
        var change = result[0];
        Assert.Equal("RemoveMe", change.AttributeId);
        Assert.Equal("OldValue", change.FromValue);
        Assert.Null(change.ToValue);
    }

    [Fact]
    public void Diff_NullToValue_ReturnsChange()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = null, ValueType = "String" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "NewValue", ValueType = "String" }
            }
        };

        var result = _diffService.Diff(before, after);

        Assert.Single(result);
        var change = result[0];
        Assert.Null(change.FromValue);
        Assert.Equal("NewValue", change.ToValue);
    }

    #endregion

    #region Ignored Attributes Tests

    [Fact]
    public void Diff_WithDefaultOptions_IgnoresSystemAttributes()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" },
                new() { AttributeId = "LastChangedDate", Value = "2026-06-08", ValueType = "DateTime" },
                new() { AttributeId = "LastChangedUser", Value = "user1", ValueType = "String" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" },
                new() { AttributeId = "LastChangedDate", Value = "2026-06-10", ValueType = "DateTime" },
                new() { AttributeId = "LastChangedUser", Value = "user2", ValueType = "String" }
            }
        };

        var result = _diffService.Diff(before, after);

        Assert.Empty(result); // All changes are in ignored attributes
    }

    [Fact]
    public void Diff_WithCustomIgnoredAttributes_ExcludesSpecified()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "OldName", ValueType = "String" },
                new() { AttributeId = "InternalField", Value = "internal1", ValueType = "String" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "NewName", ValueType = "String" },
                new() { AttributeId = "InternalField", Value = "internal2", ValueType = "String" }
            }
        };

        var options = new DiffOptions
        {
            IgnoredAttributeIds = new(StringComparer.OrdinalIgnoreCase) { "InternalField" }
        };

        var result = _diffService.Diff(before, after, options);

        Assert.Single(result);
        Assert.Equal("Name", result[0].AttributeId);
    }

    [Fact]
    public void Diff_WithIncludeAllOptions_ReturnsAllChanges()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" },
                new() { AttributeId = "LastChangedDate", Value = "2026-06-08", ValueType = "DateTime" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" },
                new() { AttributeId = "LastChangedDate", Value = "2026-06-10", ValueType = "DateTime" }
            }
        };

        var result = _diffService.Diff(before, after, DiffOptions.IncludeAll);

        Assert.Single(result);
        Assert.Equal("LastChangedDate", result[0].AttributeId);
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public void Diff_WithCaseSensitiveComparison_DetectsCase()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "test", ValueType = "String" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" }
            }
        };

        var options = new DiffOptions { CaseSensitiveComparison = true };
        var result = _diffService.Diff(before, after, options);

        Assert.Single(result); // Case difference detected
    }

    [Fact]
    public void Diff_WithCaseInsensitiveComparison_IgnoresCase()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "test", ValueType = "String" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" }
            }
        };

        var options = new DiffOptions { CaseSensitiveComparison = false };
        var result = _diffService.Diff(before, after, options);

        Assert.Empty(result); // Case difference ignored
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Diff_WithNullBefore_ThrowsArgumentNullException()
    {
        var after = new AktaRecordSnapshot { RecordId = "REC-001" };

        Assert.Throws<ArgumentNullException>(() => _diffService.Diff(null!, after));
    }

    [Fact]
    public void Diff_WithNullAfter_ThrowsArgumentNullException()
    {
        var before = new AktaRecordSnapshot { RecordId = "REC-001" };

        Assert.Throws<ArgumentNullException>(() => _diffService.Diff(before, null!));
    }

    [Fact]
    public void Diff_WithNullOptions_ThrowsArgumentNullException()
    {
        var before = new AktaRecordSnapshot { RecordId = "REC-001" };
        var after = new AktaRecordSnapshot { RecordId = "REC-001" };

        Assert.Throws<ArgumentNullException>(() => _diffService.Diff(before, after, null!));
    }

    [Fact]
    public void Diff_WithDifferentRecordIds_ReturnsEmpty()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Test", ValueType = "String" }
            }
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-002",
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Different", ValueType = "String" }
            }
        };

        var result = _diffService.Diff(before, after);

        Assert.Empty(result); // Different records, return empty
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Diff_RealWorldScenario_NodeUpdate()
    {
        // Scenario: User modifies a node's properties
        var beforeSnapshot = new AktaRecordSnapshot
        {
            RecordId = "NODE-658",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            LastChangedDate = new DateTime(2026, 6, 8, 11, 14, 00),
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Original Name", ValueType = "String" },
                new() { AttributeId = "Description", Value = "Original description", ValueType = "String" },
                new() { AttributeId = "Version", Value = "1.0", ValueType = "String" },
                new() { AttributeId = "LastChangedUser", Value = "istvan.vencz", ValueType = "String" }
            }
        };

        var afterSnapshot = new AktaRecordSnapshot
        {
            RecordId = "NODE-658",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Modified",
            LastChangedDate = new DateTime(2026, 6, 8, 11, 15, 00),
            Properties = new List<AktaRecordPropertySnapshot>
            {
                new() { AttributeId = "Name", Value = "Updated Name", ValueType = "String" },
                new() { AttributeId = "Description", Value = "Updated description", ValueType = "String" },
                new() { AttributeId = "Version", Value = "1.1", ValueType = "String" },
                new() { AttributeId = "LastChangedUser", Value = "istvan.vencz", ValueType = "String" }
            }
        };

        var result = _diffService.Diff(beforeSnapshot, afterSnapshot);

        // Should detect changes in Name, Description, Version (but not LastChangedDate or LastChangedUser due to defaults)
        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.AttributeId == "Name" && c.FromValue == "Original Name" && c.ToValue == "Updated Name");
        Assert.Contains(result, c => c.AttributeId == "Description" && c.FromValue == "Original description");
        Assert.Contains(result, c => c.AttributeId == "Version" && c.FromValue == "1.0" && c.ToValue == "1.1");
    }

    [Fact]
    public void Diff_EmptyPropertyLists_ReturnEmpty()
    {
        var before = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>()
        };

        var after = new AktaRecordSnapshot
        {
            RecordId = "REC-001",
            TypeId = "NodeType",
            TypeKind = "Node",
            RecordState = "Active",
            Properties = new List<AktaRecordPropertySnapshot>()
        };

        var result = _diffService.Diff(before, after);

        Assert.Empty(result);
    }

    #endregion
}
