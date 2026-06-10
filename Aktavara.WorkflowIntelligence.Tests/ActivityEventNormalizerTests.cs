using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aktavara.WorkflowIntelligence.Tests;

public class ActivityEventNormalizerTests
{
    private readonly ActivityEventNormalizer _normalizer;

    public ActivityEventNormalizerTests()
    {
        var mockLogger = new Mock<ILogger<ActivityEventNormalizer>>();
        var xmlExtractor = new AktaXmlExtractor(new Mock<ILogger<AktaXmlExtractor>>().Object);
        _normalizer = new ActivityEventNormalizer(xmlExtractor, mockLogger.Object);
    }

    #region Search Records Tests

    [Fact]
    public void Normalize_WithEmptyList_ReturnsEmptyList()
    {
        var result = _normalizer.Normalize(Array.Empty<RawActivityLogEntry>());
        Assert.Empty(result);
    }

    [Fact]
    public void Normalize_SearchRecordsRequest_CreatesSearchEvent()
    {
        var searchXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SearchRequest>
              <Record TypeKind="TypedSearchExpressionItem" TypeId="TypedSearchExpressionItem" RecordId="EXPR-001" State="Active">
                <Attribute AttributeId="Kind">
                  <AttributeValue ValueType="String">Node</AttributeValue>
                </Attribute>
                <Attribute AttributeId="TypeId">
                  <AttributeValue ValueType="String">MyNodeType</AttributeValue>
                </Attribute>
              </Record>
            </SearchRequest>
            """;

        var rawEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 13, 20),
            UserName = "istvan.vencz",
            SessionId = "17",
            Direction = EventType.RequestInitiated,
            ActionName = "Search records",
            RawText = searchXml,
            RawXmlPayloads = new List<string> { searchXml }
        };

        var result = _normalizer.Normalize(new[] { rawEntry });

        Assert.Single(result);
        var evt = result[0];
        Assert.Equal(EventType.SearchRecords, evt.EventType);
        Assert.Equal("Search records", evt.ActionName);
        Assert.Equal("istvan.vencz", evt.UserName);
        Assert.Equal("17", evt.SessionId);
        Assert.Equal("MyNodeType", evt.TypeId);
        Assert.Equal(RecordKind.Node, evt.RecordKind);
        Assert.True(evt.IsSuccess);
    }

    [Fact]
    public void Normalize_SearchRecordsWithEvidenceLogging()
    {
        var searchXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SearchRequest>
              <Record TypeKind="TypedSearchExpressionItem" TypeId="TypedSearchExpressionItem" RecordId="EXPR-001" State="Active">
                <Attribute AttributeId="Kind">
                  <AttributeValue ValueType="String">Path</AttributeValue>
                </Attribute>
                <Attribute AttributeId="TypeId">
                  <AttributeValue ValueType="String">PathType</AttributeValue>
                </Attribute>
              </Record>
            </SearchRequest>
            """;

        var rawEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 13, 20),
            UserName = "user",
            SessionId = "1",
            Direction = EventType.RequestInitiated,
            ActionName = "Search records",
            RawText = searchXml,
            RawXmlPayloads = new List<string> { searchXml }
        };

        var result = _normalizer.Normalize(new[] { rawEntry });

        Assert.Single(result);
        var evt = result[0];
        Assert.NotEmpty(evt.Evidence);
        Assert.Contains("Search", evt.Evidence[0]);
    }

    #endregion

    #region Open Workspace Tests

    [Fact]
    public void Normalize_OpenWorkspaceRequest_CreatesOpenEvent()
    {
        var pathXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <PathWkData>
              <Path TypeKind="Path" TypeId="PathType" RecordId="PATH-001" State="Active">
                <Attribute AttributeId="Name">
                  <AttributeValue ValueType="String">Main Path</AttributeValue>
                </Attribute>
              </Path>
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

        var rawEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 14, 00),
            UserName = "istvan.vencz",
            SessionId = "17",
            Direction = EventType.RequestInitiated,
            ActionName = "Open workspace Path",
            RawText = pathXml,
            RawXmlPayloads = new List<string> { pathXml }
        };

        var result = _normalizer.Normalize(new[] { rawEntry });

        Assert.Single(result);
        var evt = result[0];
        Assert.Equal(EventType.OpenWorkspace, evt.EventType);
        Assert.Equal("Open workspace Path", evt.ActionName);
        Assert.Equal(RecordKind.Path, evt.RecordKind);
        Assert.Equal("PATH-001", evt.RecordId);
        Assert.Equal("Main Path", evt.RecordName);
        Assert.True(evt.IsSuccess);
        Assert.Equal(3, evt.RelatedRecordIds.Count);
    }

    [Fact]
    public void Normalize_OpenWorkspaceWithMetadata()
    {
        var pathXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <PathWkData>
              <Path TypeKind="Path" TypeId="PathType" RecordId="PATH-001" State="Active">
                <Attribute AttributeId="Name">
                  <AttributeValue ValueType="String">Test Path</AttributeValue>
                </Attribute>
              </Path>
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
            </PathWkData>
            """;

        var rawEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 14, 00),
            UserName = "user",
            SessionId = "1",
            Direction = EventType.RequestInitiated,
            ActionName = "Open workspace Path",
            RawText = pathXml,
            RawXmlPayloads = new List<string> { pathXml }
        };

        var result = _normalizer.Normalize(new[] { rawEntry });

        Assert.Single(result);
        var evt = result[0];
        Assert.Equal(3, (int)evt.Metadata["node_count"]);
        Assert.Equal(1, (int)evt.Metadata["connector_count"]);
    }

    #endregion

    #region Save Records Tests

    [Fact]
    public void Normalize_SaveRecordsRequest_CreatesSaveEvent()
    {
        var saveXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SaveRequest>
              <Record TypeKind="Node" TypeId="NodeType" RecordId="NODE-001" State="Modified">
                <Attribute AttributeId="Name">
                  <AttributeValue ValueType="String">Updated Node</AttributeValue>
                </Attribute>
                <Attribute AttributeId="Description">
                  <AttributeValue ValueType="String">Updated description</AttributeValue>
                </Attribute>
              </Record>
            </SaveRequest>
            """;

        var rawEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 15, 00),
            UserName = "istvan.vencz",
            SessionId = "17",
            Direction = EventType.RequestInitiated,
            ActionName = "Save records",
            RawText = saveXml,
            RawXmlPayloads = new List<string> { saveXml }
        };

        var result = _normalizer.Normalize(new[] { rawEntry });

        Assert.Single(result);
        var evt = result[0];
        Assert.Equal(EventType.SaveRecords, evt.EventType);
        Assert.Equal("Save records", evt.ActionName);
        Assert.Equal("NODE-001", evt.RecordId);
        Assert.Equal("Updated Node", evt.RecordName);
        Assert.Equal(RecordKind.Node, evt.RecordKind);
        Assert.Equal("Modified", evt.RecordState);
        Assert.Equal(2, evt.ChangedAttributes.Count);
    }

    [Fact]
    public void Normalize_SaveRecordsWithCorrelation_UpdatesSuccessStatus()
    {
        var saveRequestXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SaveRequest>
              <Record TypeKind="Node" TypeId="NodeType" RecordId="NODE-001" State="Modified">
                <Attribute AttributeId="Name">
                  <AttributeValue ValueType="String">Updated Node</AttributeValue>
                </Attribute>
              </Record>
            </SaveRequest>
            """;

        var saveResponseXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SaveResponse>
              <Result>true</Result>
            </SaveResponse>
            """;

        var requestEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 15, 00),
            UserName = "istvan.vencz",
            SessionId = "17",
            Direction = EventType.RequestInitiated,
            ActionName = "Save records",
            RawText = saveRequestXml,
            RawXmlPayloads = new List<string> { saveRequestXml }
        };

        var responseEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 15, 01),
            UserName = "istvan.vencz",
            SessionId = "17",
            Direction = EventType.ResponseReceived,
            ActionName = "Save records",
            RawText = saveResponseXml,
            RawXmlPayloads = new List<string> { saveResponseXml }
        };

        var result = _normalizer.Normalize(new[] { requestEntry, responseEntry });

        Assert.Single(result);
        var evt = result[0];
        Assert.True(evt.IsSuccess);
        var evidenceLower = evt.Evidence.Select(e => e.ToLower()).ToList();
        Assert.Contains("success", evidenceLower.SingleOrDefault(e => e.Contains("indicates")));
        Assert.Contains("correlated_response_index", evt.Metadata.Keys);
    }

    [Fact]
    public void Normalize_SaveRecordsWithFailedResponse()
    {
        var saveRequestXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SaveRequest>
              <Record TypeKind="Node" TypeId="NodeType" RecordId="NODE-001" State="Modified"/>
            </SaveRequest>
            """;

        var failResponseXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SaveResponse>
              <Result>false</Result>
            </SaveResponse>
            """;

        var requestEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 15, 00),
            UserName = "user",
            SessionId = "1",
            Direction = EventType.RequestInitiated,
            ActionName = "Save records",
            RawText = saveRequestXml,
            RawXmlPayloads = new List<string> { saveRequestXml }
        };

        var responseEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 15, 01),
            UserName = "user",
            SessionId = "1",
            Direction = EventType.ResponseReceived,
            ActionName = "Save records",
            RawText = failResponseXml,
            RawXmlPayloads = new List<string> { failResponseXml }
        };

        var result = _normalizer.Normalize(new[] { requestEntry, responseEntry });

        Assert.Single(result);
        var evt = result[0];
        Assert.False(evt.IsSuccess);
        var evidenceLower = evt.Evidence.Select(e => e.ToLower()).ToList();
        Assert.Contains("failure", evidenceLower.SingleOrDefault(e => e.Contains("indicates")));
    }

    [Fact]
    public void Normalize_SaveMultipleRecords_CreatesSeparateEvents()
    {
        var saveXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SaveRequest>
              <Record TypeKind="Node" TypeId="NodeType" RecordId="NODE-001" State="Modified"/>
              <Record TypeKind="Node" TypeId="NodeType" RecordId="NODE-002" State="Modified"/>
              <Record TypeKind="Connector" TypeId="ConnType" RecordId="CONN-001" State="Modified"/>
            </SaveRequest>
            """;

        var rawEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 15, 00),
            UserName = "user",
            SessionId = "1",
            Direction = EventType.RequestInitiated,
            ActionName = "Save records",
            RawText = saveXml,
            RawXmlPayloads = new List<string> { saveXml }
        };

        var result = _normalizer.Normalize(new[] { rawEntry });

        Assert.Equal(3, result.Count);
        Assert.All(result, evt => Assert.Equal(EventType.SaveRecords, evt.EventType));

        var recordIds = result.Select(e => e.RecordId).ToList();
        Assert.Contains("NODE-001", recordIds);
        Assert.Contains("NODE-002", recordIds);
        Assert.Contains("CONN-001", recordIds);
    }

    #endregion

    #region Integration Tests - Complete Workflow

    [Fact]
    public void Normalize_CompleteSearchOpenSaveWorkflow()
    {
        // Step 1: Search for paths
        var searchXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SearchRequest>
              <Record TypeKind="TypedSearchExpressionItem" TypeId="TypedSearchExpressionItem" RecordId="EXPR-001" State="Active">
                <Attribute AttributeId="Kind">
                  <AttributeValue ValueType="String">Path</AttributeValue>
                </Attribute>
                <Attribute AttributeId="TypeId">
                  <AttributeValue ValueType="String">PathType</AttributeValue>
                </Attribute>
              </Record>
            </SearchRequest>
            """;

        var searchEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 13, 20),
            UserName = "istvan.vencz",
            SessionId = "17",
            Direction = EventType.RequestInitiated,
            ActionName = "Search records",
            RawText = searchXml,
            RawXmlPayloads = new List<string> { searchXml }
        };

        // Step 2: Open the found path
        var pathXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <PathWkData>
              <Path TypeKind="Path" TypeId="PathType" RecordId="PATH-001" State="Active">
                <Attribute AttributeId="Name">
                  <AttributeValue ValueType="String">Main Path</AttributeValue>
                </Attribute>
              </Path>
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

        var openEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 14, 00),
            UserName = "istvan.vencz",
            SessionId = "17",
            Direction = EventType.RequestInitiated,
            ActionName = "Open workspace Path",
            RawText = pathXml,
            RawXmlPayloads = new List<string> { pathXml }
        };

        // Step 3: Save modified node
        var saveXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SaveRequest>
              <Record TypeKind="Node" TypeId="NodeType" RecordId="N1" State="Modified">
                <Attribute AttributeId="Name">
                  <AttributeValue ValueType="String">Updated Node</AttributeValue>
                </Attribute>
              </Record>
            </SaveRequest>
            """;

        var saveEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 15, 00),
            UserName = "istvan.vencz",
            SessionId = "17",
            Direction = EventType.RequestInitiated,
            ActionName = "Save records",
            RawText = saveXml,
            RawXmlPayloads = new List<string> { saveXml }
        };

        // Step 4: Save success response
        var saveResponseXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SaveResponse>
              <Result>true</Result>
            </SaveResponse>
            """;

        var saveResponseEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 15, 01),
            UserName = "istvan.vencz",
            SessionId = "17",
            Direction = EventType.ResponseReceived,
            ActionName = "Save records",
            RawText = saveResponseXml,
            RawXmlPayloads = new List<string> { saveResponseXml }
        };

        var entries = new[] { searchEntry, openEntry, saveEntry, saveResponseEntry };
        var result = _normalizer.Normalize(entries);

        // Verify we get the expected events
        Assert.Equal(3, result.Count);

        // Check search event
        var searchEvt = result.FirstOrDefault(e => e.EventType == EventType.SearchRecords);
        Assert.NotNull(searchEvt);
        Assert.Equal(RecordKind.Path, searchEvt.RecordKind);

        // Check open event
        var openEvt = result.FirstOrDefault(e => e.EventType == EventType.OpenWorkspace);
        Assert.NotNull(openEvt);
        Assert.Equal("PATH-001", openEvt.RecordId);
        Assert.Equal(3, openEvt.RelatedRecordIds.Count);

        // Check save event with success correlation
        var saveEvt = result.FirstOrDefault(e => e.EventType == EventType.SaveRecords);
        Assert.NotNull(saveEvt);
        Assert.Equal("N1", saveEvt.RecordId);
        Assert.True(saveEvt.IsSuccess);
        Assert.Single(saveEvt.ChangedAttributes);
    }

    [Fact]
    public void Normalize_UnknownActionName_SkipsEntry()
    {
        var unknownEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 13, 20),
            UserName = "user",
            SessionId = "1",
            Direction = EventType.RequestInitiated,
            ActionName = "Unknown action that does not exist",
            RawText = "some content",
            RawXmlPayloads = new List<string> { "<Root/>" }
        };

        var result = _normalizer.Normalize(new[] { unknownEntry });

        Assert.Empty(result);
    }

    [Fact]
    public void Normalize_ResponseEntriesWithoutRequest_Skipped()
    {
        var responseEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 13, 21),
            UserName = "user",
            SessionId = "1",
            Direction = EventType.ResponseReceived,
            ActionName = "Search records",
            RawText = "<SearchResponse/>",
            RawXmlPayloads = new List<string> { "<SearchResponse/>" }
        };

        var result = _normalizer.Normalize(new[] { responseEntry });

        // Responses are skipped if they can't be correlated
        Assert.Empty(result);
    }

    [Fact]
    public void Normalize_PreservesSessionAndUserInfo()
    {
        var searchXml = """
            <?xml version="1.0" encoding="utf-16"?>
            <SearchRequest>
              <Record TypeKind="TypedSearchExpressionItem" TypeId="TypedSearchExpressionItem" RecordId="EXPR-001" State="Active">
                <Attribute AttributeId="Kind">
                  <AttributeValue ValueType="String">Node</AttributeValue>
                </Attribute>
                <Attribute AttributeId="TypeId">
                  <AttributeValue ValueType="String">TestType</AttributeValue>
                </Attribute>
              </Record>
            </SearchRequest>
            """;

        var rawEntry = new RawActivityLogEntry
        {
            Timestamp = new DateTime(2026, 6, 8, 11, 13, 20),
            UserName = "john.smith",
            SessionId = "42",
            Direction = EventType.RequestInitiated,
            ActionName = "Search records",
            RawText = searchXml,
            RawXmlPayloads = new List<string> { searchXml }
        };

        var result = _normalizer.Normalize(new[] { rawEntry });

        Assert.Single(result);
        var evt = result[0];
        Assert.Equal("john.smith", evt.UserName);
        Assert.Equal("42", evt.SessionId);
    }

    #endregion
}
