using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aktavara.WorkflowIntelligence.Tests;

public class AktaXmlExtractorTests
{
    private readonly AktaXmlExtractor _extractor;

    public AktaXmlExtractorTests()
    {
        var mockLogger = new Mock<ILogger<AktaXmlExtractor>>();
        _extractor = new AktaXmlExtractor(mockLogger.Object);
    }

    #region ExtractRecords Tests

    [Fact]
    public void ExtractRecords_WithEmptyString_ReturnsEmptyList()
    {
        var result = _extractor.ExtractRecords(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractRecords_WithNullString_ReturnsEmptyList()
    {
        var result = _extractor.ExtractRecords(null!);
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractRecords_WithInvalidXml_ReturnsEmptyList()
    {
        var xml = "this is not valid xml";
        var result = _extractor.ExtractRecords(xml);
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractRecords_WithSingleRecord_ExtractsSuccessfully()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SearchResponse>
              <Record TypeKind="Node" TypeId="MyNode" RecordId="NODE-001" State="Active">
                <Attribute AttributeId="Name">
                  <AttributeValue ValueType="String">Test Node</AttributeValue>
                </Attribute>
                <Attribute AttributeId="Description">
                  <AttributeValue ValueType="String">A test node</AttributeValue>
                </Attribute>
              </Record>
            </SearchResponse>
            """;

        var result = _extractor.ExtractRecords(xml);

        Assert.Single(result);
        var record = result[0];
        Assert.Equal("Node", record.TypeKind);
        Assert.Equal("MyNode", record.TypeId);
        Assert.Equal("NODE-001", record.RecordId);
        Assert.Equal("Active", record.RecordState);
        Assert.Equal(2, record.Properties.Count);
    }

    [Fact]
    public void ExtractRecords_WithMultipleRecords_ExtractsAll()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SearchResponse>
              <Record TypeKind="Node" TypeId="TypeA" RecordId="REC-001" State="Active"/>
              <Record TypeKind="Node" TypeId="TypeB" RecordId="REC-002" State="Draft"/>
              <Record TypeKind="Connector" TypeId="TypeC" RecordId="REC-003" State="Active"/>
            </SearchResponse>
            """;

        var result = _extractor.ExtractRecords(xml);

        Assert.Equal(3, result.Count);
        Assert.Equal("REC-001", result[0].RecordId);
        Assert.Equal("REC-002", result[1].RecordId);
        Assert.Equal("REC-003", result[2].RecordId);
    }

    [Fact]
    public void ExtractRecords_WithProperties_ExtractsAttributeValues()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <Record TypeKind="Node" TypeId="TestType" RecordId="REC-001" State="Active">
              <Attribute AttributeId="Name">
                <AttributeValue ValueType="String">My Record</AttributeValue>
              </Attribute>
              <Attribute AttributeId="Revision">
                <AttributeValue ValueType="Integer">42</AttributeValue>
              </Attribute>
              <Attribute AttributeId="IsActive">
                <AttributeValue ValueType="Boolean">true</AttributeValue>
              </Attribute>
            </Record>
            """;

        var result = _extractor.ExtractRecords(xml);

        Assert.Single(result);
        var record = result[0];
        Assert.Equal(3, record.Properties.Count);

        var nameProp = record.FindProperty("Name");
        Assert.NotNull(nameProp);
        Assert.Equal("My Record", nameProp.Value);
        Assert.Equal("String", nameProp.ValueType);

        var revisionProp = record.FindProperty("Revision");
        Assert.NotNull(revisionProp);
        Assert.Equal("42", revisionProp.Value);
    }

    [Fact]
    public void ExtractRecords_WithLastChangedDate_ParsesDateTime()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <Record TypeKind="Node" TypeId="Test" RecordId="REC-001" State="Active" LastChangedDate="2026-06-08T11:13:20"/>
            """;

        var result = _extractor.ExtractRecords(xml);

        Assert.Single(result);
        var record = result[0];
        Assert.NotNull(record.LastChangedDate);
        Assert.Equal(new DateTime(2026, 6, 8, 11, 13, 20), record.LastChangedDate.Value);
    }

    [Fact]
    public void ExtractRecords_WithStageId_ExtractsStageInfo()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <Record TypeKind="Node" TypeId="Test" RecordId="REC-001" State="Active" StageId="STAGE-05"/>
            """;

        var result = _extractor.ExtractRecords(xml);

        Assert.Single(result);
        Assert.Equal("STAGE-05", result[0].StageId);
    }

    [Fact]
    public void ExtractRecords_WithXsiType_CapturesTypeInfo()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <Record TypeKind="Node" TypeId="Test" RecordId="REC-001" State="Active">
              <Attribute AttributeId="ComplexValue" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:type="ComplexType">
                <AttributeValue ValueType="Object">SomeValue</AttributeValue>
              </Attribute>
            </Record>
            """;

        var result = _extractor.ExtractRecords(xml);

        Assert.Single(result);
        var record = result[0];
        var prop = record.FindProperty("ComplexValue");
        Assert.NotNull(prop);
        Assert.NotNull(prop.XsiType);
    }

    #endregion

    #region ExtractPageInfo Tests

    [Fact]
    public void ExtractPageInfo_WithValidPageInfo_ExtractsSuccessfully()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SearchResponse>
              <PageInfo>
                <PageNumber>1</PageNumber>
                <TotalRecords>100</TotalRecords>
                <PageSize>10</PageSize>
                <StartIndex>0</StartIndex>
                <HasMorePages>true</HasMorePages>
              </PageInfo>
            </SearchResponse>
            """;

        var result = _extractor.ExtractPageInfo(xml);

        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(100, result.TotalRecords);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.StartIndex);
        Assert.True(result.HasMorePages);
    }

    [Fact]
    public void ExtractPageInfo_CalculatesTotalPages()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <PageInfo>
              <PageNumber>2</PageNumber>
              <TotalRecords>105</TotalRecords>
              <PageSize>10</PageSize>
              <StartIndex>10</StartIndex>
              <HasMorePages>true</HasMorePages>
            </PageInfo>
            """;

        var result = _extractor.ExtractPageInfo(xml);

        Assert.NotNull(result);
        Assert.Equal(11, result.TotalPages); // ceil(105 / 10)
        Assert.Equal(10, result.RecordCountOnPage); // min(10, 105 - 10)
    }

    [Fact]
    public void ExtractPageInfo_WithNoPageInfo_ReturnsNull()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SearchResponse>
              <Records/>
            </SearchResponse>
            """;

        var result = _extractor.ExtractPageInfo(xml);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractPageInfo_WithEmptyXml_ReturnsNull()
    {
        var result = _extractor.ExtractPageInfo(string.Empty);
        Assert.Null(result);
    }

    #endregion

    #region ExtractPathWorkspace Tests

    [Fact]
    public void ExtractPathWorkspace_WithCompletePathData_ExtractsSuccessfully()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <PathWkData>
              <Path TypeKind="Path" TypeId="PathType" RecordId="PATH-001" State="Active"/>
              <StartVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="NODE-001" State="Active"/>
              </StartVertex>
              <StartVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="NODE-002" State="Active"/>
              </StartVertex>
              <Edge StartNodeRecordId="NODE-001" EndNodeRecordId="NODE-002" ConnectorRecordId="CONN-001">
                <Connector>
                  <Record TypeKind="Connector" TypeId="ConnType" RecordId="CONN-001" State="Active"/>
                </Connector>
              </Edge>
            </PathWkData>
            """;

        var result = _extractor.ExtractPathWorkspace(xml);

        Assert.NotNull(result);
        Assert.Equal("PATH-001", result.PathRecord.RecordId);
        Assert.Equal("Path", result.PathRecord.TypeKind);
        Assert.Equal(2, result.Nodes.Count);
        Assert.Single(result.Connectors);
        Assert.Single(result.Edges);
    }

    [Fact]
    public void ExtractPathWorkspace_WithNodes_ExtractsNodeList()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <PathWkData>
              <Path TypeKind="Path" TypeId="PathType" RecordId="PATH-001" State="Active"/>
              <StartVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="NODE-001" State="Active">
                  <Attribute AttributeId="Name">
                    <AttributeValue>Start Node</AttributeValue>
                  </Attribute>
                </Record>
              </StartVertex>
              <EndVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="NODE-002" State="Active">
                  <Attribute AttributeId="Name">
                    <AttributeValue>End Node</AttributeValue>
                  </Attribute>
                </Record>
              </EndVertex>
            </PathWkData>
            """;

        var result = _extractor.ExtractPathWorkspace(xml);

        Assert.NotNull(result);
        Assert.Equal(2, result.Nodes.Count);

        var node1 = result.FindNode("NODE-001");
        Assert.NotNull(node1);
        Assert.Equal("Node", node1.TypeKind);

        var node2 = result.FindNode("NODE-002");
        Assert.NotNull(node2);
        Assert.Equal("Node", node2.TypeKind);
    }

    [Fact]
    public void ExtractPathWorkspace_WithEdges_ExtractsRelationships()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <PathWkData>
              <Path TypeKind="Path" TypeId="PathType" RecordId="PATH-001" State="Active"/>
              <StartVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="N1" State="Active"/>
              </StartVertex>
              <StartVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="N2" State="Active"/>
              </StartVertex>
              <StartVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="N3" State="Active"/>
              </StartVertex>
              <Edge StartNodeRecordId="N1" EndNodeRecordId="N2" ConnectorRecordId="C1">
                <Connector>
                  <Record TypeKind="Connector" TypeId="ConnType" RecordId="C1" State="Active"/>
                </Connector>
              </Edge>
              <Edge StartNodeRecordId="N2" EndNodeRecordId="N3" ConnectorRecordId="C2">
                <Connector>
                  <Record TypeKind="Connector" TypeId="ConnType" RecordId="C2" State="Active"/>
                </Connector>
              </Edge>
            </PathWkData>
            """;

        var result = _extractor.ExtractPathWorkspace(xml);

        Assert.NotNull(result);
        Assert.Equal(3, result.Nodes.Count);
        Assert.Equal(2, result.Edges.Count);

        var edge1 = result.Edges[0];
        Assert.Equal("N1", edge1.StartNodeRecordId);
        Assert.Equal("N2", edge1.EndNodeRecordId);
        Assert.Equal("C1", edge1.ConnectorRecordId);

        var edgesForN1 = result.FindEdgesForNode("N1");
        Assert.Single(edgesForN1);
    }

    [Fact]
    public void ExtractPathWorkspace_WithNoPathData_ReturnsNull()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SearchResponse/>
            """;

        var result = _extractor.ExtractPathWorkspace(xml);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractPathWorkspace_CalculatesEntityCounts()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <PathWkData>
              <Path TypeKind="Path" TypeId="PathType" RecordId="PATH-001" State="Active"/>
              <StartVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="N1" State="Active"/>
              </StartVertex>
              <StartVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="N2" State="Active"/>
              </StartVertex>
              <Edge StartNodeRecordId="N1" EndNodeRecordId="N2" ConnectorRecordId="C1">
                <Connector>
                  <Record TypeKind="Connector" TypeId="ConnType" RecordId="C1" State="Active"/>
                </Connector>
              </Edge>
            </PathWkData>
            """;

        var result = _extractor.ExtractPathWorkspace(xml);

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalEntityCount); // 2 nodes + 1 connector
        Assert.Single(result.Edges);
    }

    #endregion

    #region ExtractBooleanResult Tests

    [Fact]
    public void ExtractBooleanResult_WithValidResult_ExtractsTrue()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <OperationResponse>
              <Result>true</Result>
            </OperationResponse>
            """;

        var result = _extractor.ExtractBooleanResult(xml);

        Assert.True(result);
    }

    [Fact]
    public void ExtractBooleanResult_WithValidResult_ExtractsFalse()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <OperationResponse>
              <Result>false</Result>
            </OperationResponse>
            """;

        var result = _extractor.ExtractBooleanResult(xml);

        Assert.False(result);
    }

    [Fact]
    public void ExtractBooleanResult_WithIsSuccess_ExtractsValue()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <Response>
              <IsSuccess>true</IsSuccess>
            </Response>
            """;

        var result = _extractor.ExtractBooleanResult(xml);

        Assert.True(result);
    }

    [Fact]
    public void ExtractBooleanResult_WithNoResult_ReturnsNull()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <Response>
              <Message>Operation completed</Message>
            </Response>
            """;

        var result = _extractor.ExtractBooleanResult(xml);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractBooleanResult_WithEmptyXml_ReturnsNull()
    {
        var result = _extractor.ExtractBooleanResult(string.Empty);
        Assert.Null(result);
    }

    [Fact]
    public void ExtractBooleanResult_WithRootAsBoolean_ExtractsValue()
    {
        var xml = "<Response>true</Response>";

        var result = _extractor.ExtractBooleanResult(xml);

        Assert.True(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ExtractRecords_GetPropertyValue_ReturnsCorrectValue()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <Record TypeKind="Node" TypeId="Test" RecordId="REC-001" State="Active">
              <Attribute AttributeId="Name">
                <AttributeValue>Test Name</AttributeValue>
              </Attribute>
            </Record>
            """;

        var result = _extractor.ExtractRecords(xml);
        var record = result[0];

        var value = record.GetPropertyValue("Name");
        Assert.Equal("Test Name", value);
    }

    [Fact]
    public void PathWorkspace_GetSummary_FormatsCorrectly()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <PathWkData>
              <Path TypeKind="Path" TypeId="PathType" RecordId="PATH-001" State="Active"/>
              <StartVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="N1" State="Active"/>
              </StartVertex>
              <StartVertex>
                <Record TypeKind="Node" TypeId="NodeType" RecordId="N2" State="Active"/>
              </StartVertex>
              <Edge StartNodeRecordId="N1" EndNodeRecordId="N2" ConnectorRecordId="C1">
                <Connector>
                  <Record TypeKind="Connector" TypeId="ConnType" RecordId="C1" State="Active"/>
                </Connector>
              </Edge>
            </PathWkData>
            """;

        var result = _extractor.ExtractPathWorkspace(xml);
        var summary = result!.GetSummary();

        Assert.Contains("PATH-001", summary);
        Assert.Contains("Nodes: 2", summary);
        Assert.Contains("Connectors: 1", summary);
        Assert.Contains("Edges: 1", summary);
    }

    [Fact]
    public void Edge_GetSummary_FormatsRelationship()
    {
        var edge = new AktaEdgeSnapshot
        {
            StartNodeRecordId = "N1",
            EndNodeRecordId = "N2",
            ConnectorRecordId = "C1"
        };

        var summary = edge.GetSummary();

        Assert.Equal("N1 -[C1]-> N2", summary);
    }

    #endregion
}
