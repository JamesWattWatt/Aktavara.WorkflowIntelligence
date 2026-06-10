using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Aktavara.WorkflowIntelligence.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aktavara.WorkflowIntelligence.Tests;

public class ActivityLogParserTests
{
    private readonly ActivityLogParser _parser;

    public ActivityLogParserTests()
    {
        var mockLogger = new Mock<ILogger<ActivityLogParser>>();
        _parser = new ActivityLogParser(mockLogger.Object);
    }

    [Fact]
    public void Parse_WithEmptyString_ReturnsEmptyList()
    {
        var result = _parser.Parse(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_WithNullString_ReturnsEmptyList()
    {
        var result = _parser.Parse(null!);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_WithWhitespaceOnly_ReturnsEmptyList()
    {
        var result = _parser.Parse("   \n\n   ");
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_SearchRecordsRequest_ParsesSuccessfully()
    {
        var logContent = """
            [2026-06-08 11:13:20] istvan.vencz (17): Request: Search records
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <SearchRequest>
              <Criteria>
                <Name>Test*</Name>
              </Criteria>
            </SearchRequest>
            """;

        var result = _parser.Parse(logContent);

        Assert.Single(result);
        var entry = result[0];

        Assert.Equal(new DateTime(2026, 6, 8, 11, 13, 20), entry.Timestamp);
        Assert.Equal("istvan.vencz", entry.UserName);
        Assert.Equal("17", entry.SessionId);
        Assert.Equal(EventType.RequestInitiated, entry.Direction);
        Assert.Equal("Search records", entry.ActionName);
        Assert.Single(entry.RawXmlPayloads);

        var xml = entry.RawXmlPayloads[0];
        Assert.Contains("<?xml version=\"1.0\" encoding=\"utf-16\"?>", xml);
        Assert.Contains("<SearchRequest>", xml);
        Assert.Contains("<Name>Test*</Name>", xml);
    }

    [Fact]
    public void Parse_SearchRecordsResponse_ParsesSuccessfully()
    {
        var logContent = """
            [2026-06-08 11:13:21] istvan.vencz (17): Response: Search records
            Output:
            <?xml version="1.0" encoding="utf-16"?>
            <SearchResponse>
              <Results>
                <Record>
                  <Id>REC001</Id>
                  <Name>Test Record</Name>
                </Record>
              </Results>
            </SearchResponse>
            """;

        var result = _parser.Parse(logContent);

        Assert.Single(result);
        var entry = result[0];

        Assert.Equal(EventType.ResponseReceived, entry.Direction);
        Assert.Equal("Search records", entry.ActionName);
        Assert.Single(entry.RawXmlPayloads);
        Assert.Contains("<SearchResponse>", entry.RawXmlPayloads[0]);
    }

    [Fact]
    public void Parse_OpenWorkspacePathRequest_ParsesSuccessfully()
    {
        var logContent = """
            [2026-06-08 11:14:00] istvan.vencz (17): Request: Open workspace Path
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <OpenRequest>
              <Workspace>Design</Workspace>
              <Path>/Projects/Active/Project1</Path>
            </OpenRequest>
            """;

        var result = _parser.Parse(logContent);

        Assert.Single(result);
        var entry = result[0];

        Assert.Equal("Open workspace Path", entry.ActionName);
        Assert.Single(entry.RawXmlPayloads);
        Assert.Contains("<Workspace>Design</Workspace>", entry.RawXmlPayloads[0]);
    }

    [Fact]
    public void Parse_SaveRecordsRequest_ParsesSuccessfully()
    {
        var logContent = """
            [2026-06-08 11:15:00] istvan.vencz (17): Request: Save records
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <SaveRequest>
              <Records>
                <Record>
                  <Id>REC001</Id>
                  <Name>Updated Record</Name>
                  <Status>Active</Status>
                </Record>
              </Records>
            </SaveRequest>
            """;

        var result = _parser.Parse(logContent);

        Assert.Single(result);
        var entry = result[0];

        Assert.Equal("Save records", entry.ActionName);
        Assert.Single(entry.RawXmlPayloads);
        Assert.Contains("<SaveRequest>", entry.RawXmlPayloads[0]);
        Assert.Contains("<Status>Active</Status>", entry.RawXmlPayloads[0]);
    }

    [Fact]
    public void Parse_MultipleLogEntries_ParsesAllSuccessfully()
    {
        var logContent = """
            [2026-06-08 11:13:20] istvan.vencz (17): Request: Search records
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <SearchRequest>
              <Name>Test*</Name>
            </SearchRequest>

            [2026-06-08 11:13:21] istvan.vencz (17): Response: Search records
            Output:
            <?xml version="1.0" encoding="utf-16"?>
            <SearchResponse>
              <Results>Record1</Results>
            </SearchResponse>

            [2026-06-08 11:14:00] istvan.vencz (17): Request: Save records
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <SaveRequest>
              <RecordId>REC001</RecordId>
            </SaveRequest>
            """;

        var result = _parser.Parse(logContent);

        Assert.Equal(3, result.Count);
        Assert.Equal("Search records", result[0].ActionName);
        Assert.Equal(EventType.RequestInitiated, result[0].Direction);
        Assert.Equal("Search records", result[1].ActionName);
        Assert.Equal(EventType.ResponseReceived, result[1].Direction);
        Assert.Equal("Save records", result[2].ActionName);
    }

    [Fact]
    public void Parse_MultipleInputOutputBlocks_ParsesBothPayloads()
    {
        var logContent = """
            [2026-06-08 11:16:00] istvan.vencz (17): Request: Complex operation
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <Request1>
              <Data>First</Data>
            </Request1>
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <Request2>
              <Data>Second</Data>
            </Request2>
            """;

        var result = _parser.Parse(logContent);

        Assert.Single(result);
        var entry = result[0];
        Assert.Equal(2, entry.RawXmlPayloads.Count);
        Assert.Contains("<Request1>", entry.RawXmlPayloads[0]);
        Assert.Contains("<Request2>", entry.RawXmlPayloads[1]);
    }

    [Fact]
    public void Parse_WithBlankLinesInXml_PreservesXmlStructure()
    {
        var logContent = """
            [2026-06-08 11:17:00] istvan.vencz (17): Request: Multi-line XML
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <Request>
              <Element1>
                <NestedElement>Value</NestedElement>
              </Element1>

              <Element2>AnotherValue</Element2>
            </Request>
            """;

        var result = _parser.Parse(logContent);

        Assert.Single(result);
        var entry = result[0];
        Assert.Single(entry.RawXmlPayloads);

        var xml = entry.RawXmlPayloads[0];
        Assert.Contains("<Element1>", xml);
        Assert.Contains("<NestedElement>Value</NestedElement>", xml);
        Assert.Contains("<Element2>AnotherValue</Element2>", xml);
    }

    [Fact]
    public void Parse_WithDifferentDateFormat_ParsesCorrectly()
    {
        var logContent = """
            [2026-01-01 00:00:00] user.name (99): Request: Test action
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <Test/>
            """;

        var result = _parser.Parse(logContent);

        Assert.Single(result);
        var entry = result[0];
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0), entry.Timestamp);
    }

    [Fact]
    public void Parse_WithDifferentUsernames_ParsesCorrectly()
    {
        var logContent = """
            [2026-06-08 11:00:00] john.doe (1): Request: Action
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <Test/>

            [2026-06-08 11:01:00] jane.smith (999): Request: Action
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <Test/>
            """;

        var result = _parser.Parse(logContent);

        Assert.Equal(2, result.Count);
        Assert.Equal("john.doe", result[0].UserName);
        Assert.Equal("1", result[0].SessionId);
        Assert.Equal("jane.smith", result[1].UserName);
        Assert.Equal("999", result[1].SessionId);
    }

    [Fact]
    public void Parse_PreservesRawText()
    {
        var logContent = """
            [2026-06-08 11:13:20] istvan.vencz (17): Request: Test
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <Test>Content</Test>
            """;

        var result = _parser.Parse(logContent);

        Assert.Single(result);
        var entry = result[0];
        Assert.Equal(logContent.Trim(), entry.RawText.Trim());
    }

    [Fact]
    public async Task ParseFileAsync_WithValidFile_ParsesSuccessfully()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var logContent = """
                [2026-06-08 11:13:20] istvan.vencz (17): Request: Search records
                Input:
                <?xml version="1.0" encoding="utf-16"?>
                <SearchRequest/>
                """;

            await File.WriteAllTextAsync(tempFile, logContent);
            var result = await _parser.ParseFileAsync(tempFile);

            Assert.Single(result);
            Assert.Equal("Search records", result[0].ActionName);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _parser.ParseFileAsync("/nonexistent/path/file.log"));
    }

    [Fact]
    public void Parse_RawTextContainsFullEntry()
    {
        var logContent = """
            [2026-06-08 11:13:20] istvan.vencz (17): Request: Search records
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <SearchRequest>
              <Query>Test</Query>
            </SearchRequest>
            """;

        var result = _parser.Parse(logContent);

        Assert.Single(result);
        var entry = result[0];

        Assert.Contains("[2026-06-08 11:13:20]", entry.RawText);
        Assert.Contains("istvan.vencz", entry.RawText);
        Assert.Contains("(17)", entry.RawText);
        Assert.Contains("Request", entry.RawText);
        Assert.Contains("Search records", entry.RawText);
        Assert.Contains("<SearchRequest>", entry.RawText);
    }

    [Fact]
    public void Parse_ActionNameWithMultipleWords_ParsesCorrectly()
    {
        var logContent = """
            [2026-06-08 11:13:20] istvan.vencz (17): Request: Open workspace Path with permissions
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <Request/>
            """;

        var result = _parser.Parse(logContent);

        Assert.Single(result);
        Assert.Equal("Open workspace Path with permissions", result[0].ActionName);
    }

    [Fact]
    public void Parse_RequestAndResponsePair_BothParsedCorrectly()
    {
        var logContent = """
            [2026-06-08 11:13:20] istvan.vencz (17): Request: Validate records
            Input:
            <?xml version="1.0" encoding="utf-16"?>
            <ValidateRequest>
              <RecordIds>123,456</RecordIds>
            </ValidateRequest>

            [2026-06-08 11:13:21] istvan.vencz (17): Response: Validate records
            Output:
            <?xml version="1.0" encoding="utf-16"?>
            <ValidateResponse>
              <IsValid>true</IsValid>
              <Errors/>
            </ValidateResponse>
            """;

        var result = _parser.Parse(logContent);

        Assert.Equal(2, result.Count);

        var request = result[0];
        Assert.Equal(EventType.RequestInitiated, request.Direction);
        Assert.Equal("Validate records", request.ActionName);
        Assert.Contains("<ValidateRequest>", request.RawXmlPayloads[0]);

        var response = result[1];
        Assert.Equal(EventType.ResponseReceived, response.Direction);
        Assert.Equal("Validate records", response.ActionName);
        Assert.Contains("<ValidateResponse>", response.RawXmlPayloads[0]);
    }
}
