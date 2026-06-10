namespace Aktavara.WorkflowIntelligence.Core.Models;

/// <summary>
/// Complete TypeKind enumeration from Aktavara schema.
/// Represents the fundamental record types in the Aktavara system.
/// Source: Akta.Bec.Model.TypeKind from Swagger documentation.
/// </summary>
public enum AktavaraTypeKind
{
    None = 0,
    Node = 1,
    Connector = 2,
    Tag = 5,
    Collection = 6,
    Diagram = 10,
    Path = 13,
    Topology = 14,
    Carrier = 15,
    Schema = 16,
    BranchNode = 17,
    BranchConnector = 18,
    BranchPath = 19,
    BranchTopology = 20,
    BranchGhost = 21,
    BranchTag = 22
}

/// <summary>
/// URF (Unified Record Format) TypeKind subset for workspace communication.
/// Source: Akta.Bec.Communicator.URFTypeKind from Swagger documentation.
/// </summary>
public enum UrfTypeKind
{
    None = 0,
    Node = 1,
    Connector = 2,
    Path = 13,
    Topology = 14,
    BranchNode = 17,
    TypeCategory = 20
}

/// <summary>
/// IPAM TypeKind enumeration for IP Address Management records.
/// Source: Akta.Bec.IPAM.IPAMTypeKind from Swagger documentation.
/// </summary>
public enum IpamTypeKind
{
    Network = 1,
    Scope = 2,
    Range = 3,
    Address = 4
}

/// <summary>
/// Workspace type flags from Aktavara system.
/// Multiple workspace types can be combined with bitwise OR operations.
/// Source: Akta.Bec.Common.WorkspaceType from Swagger documentation.
/// </summary>
[Flags]
public enum AktavaraWorkspaceType
{
    Default = 0,
    Path = 1,
    TagAllocation = 2,
    Graphics = 4,
    Diagram = 8,
    Properties = 16,
    Spreadsheet = 32,
    Collection = 64,
    Explorer = 128,
    NodeEditor = 256,
    Bookmarks = 512,
    Topology = 1024,
    Connections = 2048,
    Routing = 4096,
    Carrier = 8192,
    ResourceTemplates = 16384,
    RecordStages = 32768,
    Schema = 65536,
    GisMap = 131072,
    ExternalExplorer = 262144,
    FlexGrid = 524288,
    Map = 1048576,
    NetworkMap = 2097152
}

/// <summary>
/// Update action types for record modifications.
/// Source: Akta.Bec.Communicator.UpdateAction from Swagger documentation.
/// </summary>
public enum AktavaraUpdateAction
{
    Attributes = 0,
    Content = 1,
    AttributesContent = 2,
    ClearTagAllocations = 3
}

/// <summary>
/// Reconciliation search filter type kinds.
/// Source: Akta.Bec.Communicator.Reconciliation.ReconciliationSearchFilterTypeKind from Swagger.
/// </summary>
public enum ReconciliationSearchFilterTypeKind
{
    // Values not explicitly defined in swagger - would need additional investigation
}

/// <summary>
/// Standardized request/response type constants for workspace access patterns.
/// These represent the primary request types used in the Aktavara WebAPI.
/// Source: Akta.WebAPI.Model from Swagger documentation.
/// </summary>
public static class WorkspaceRequestTypes
{
    // Path workspace requests
    public const string GetPathWorkspaceDataRequest = "Akta.WebAPI.Model.PathComposer.GetPathWorkspaceDataRequest";
    public const string GetPathWorkspaceDataResponse = "Akta.WebAPI.Model.PathComposer.GetPathWorkspaceDataResponse";
    public const string GetPathWsDataRequest = "Akta.WebAPI.Model.PathComposer.GetPathWsDataRequest";
    public const string GetPathWsDataResponse = "Akta.WebAPI.Model.PathComposer.GetPathWsDataResponse";

    // Topology workspace requests
    public const string GetWsTopologyRequest = "Akta.WebAPI.Model.TopologyComposer.GetWsTopologyRequest";
    public const string GetWsTopologyResponse = "Akta.WebAPI.Model.TopologyComposer.GetWsTopologyResponse";
    public const string GetTopologyDataRequest = "Akta.WebAPI.Model.TopologyComposer.GetTopologyDataRequest";
    public const string GetTopologyDataResponse = "Akta.WebAPI.Model.TopologyComposer.GetTopologyDataResponse";

    // Diagram workspace requests
    public const string GetWsDiagramRequest = "Akta.WebAPI.Model.DiagramComposer.GetWsDiagramRequest";
    public const string GetWsDiagramResponse = "Akta.WebAPI.Model.DiagramComposer.GetWsDiagramResponse";
    public const string GetDiagramDataRequest = "Akta.WebAPI.Model.DiagramComposer.GetDiagramDataRequest";
    public const string GetDiagramDataResponse = "Akta.WebAPI.Model.DiagramComposer.GetDiagramDataResponse";

    // Carrier workspace requests
    public const string GetWsCarrierRequest = "Akta.WebAPI.Model.CarrierComposer.GetWsCarrierRequest";
    public const string GetWsCarrierResponse = "Akta.WebAPI.Model.CarrierComposer.GetWsCarrierResponse";

    // Schema workspace requests
    public const string GetWsSchemaRequest = "Akta.WebAPI.Model.SchemaComposer.GetWsSchemaRequest";
    public const string GetWsSchemaResponse = "Akta.WebAPI.Model.SchemaComposer.GetWsSchemaResponse";

    // Branch/Fork topology requests
    public const string GetBranchTopologyRequest = "Akta.WebAPI.Model.BranchComposer.GetBranchTopologyRequest";
    public const string GetBranchTopologyResponse = "Akta.WebAPI.Model.BranchComposer.GetBranchTopologyResponse";
}

/// <summary>
/// Common request/response type patterns for data queries.
/// </summary>
public static class DataQueryRequestTypes
{
    // Path entity/options queries
    public const string GetPathEntitiesRequest = "Akta.WebAPI.Model.PathModeler.GetPathEntitiesRequest";
    public const string GetPathEntitiesResponse = "Akta.WebAPI.Model.PathModeler.GetPathEntitiesResponse";
    public const string GetPathOptionsRequest = "Akta.WebAPI.Model.PathModeler.GetPathOptionsRequest";
    public const string GetPathOptionsResponse = "Akta.WebAPI.Model.PathModeler.GetPathOptionsResponse";

    // Topology entity/options queries
    public const string GetTopologyEntitiesRequest = "Akta.WebAPI.Model.TopologyModeler.GetTopologyEntitiesRequest";
    public const string GetTopologyEntitiesResponse = "Akta.WebAPI.Model.TopologyModeler.GetTopologyEntitiesResponse";
    public const string GetTopologyOptionsRequest = "Akta.WebAPI.Model.TopologyModeler.GetTopologyOptionsRequest";
    public const string GetTopologyOptionsResponse = "Akta.WebAPI.Model.TopologyModeler.GetTopologyOptionsResponse";

    // Diagram entity/options queries
    public const string GetDiagramEntitiesRequest = "Akta.WebAPI.Model.DiagramAccessor.GetEntitiesRequest";
    public const string GetDiagramEntitiesResponse = "Akta.WebAPI.Model.DiagramAccessor.GetEntitiesResponse";
    public const string GetDiagramOptionsRequest = "Akta.WebAPI.Model.DiagramAccessor.GetOptionsRequest";
    public const string GetDiagramOptionsResponse = "Akta.WebAPI.Model.DiagramAccessor.GetOptionsResponse";

    // Type/Schema queries
    public const string GetTypesByTypeKindRequest = "Akta.WebAPI.Model.TypeModeler.GetTypesByTypeKindRequest";
    public const string GetTypesByTypeKindResponse = "Akta.WebAPI.Model.TypeModeler.GetTypesByTypeKindResponse";
}

/// <summary>
/// Helper class for type conversion and validation.
/// </summary>
public static class AktavaraTypeHelper
{
    /// <summary>
    /// Converts a string TypeKind value to the enumeration.
    /// Falls back to None if the value is not recognized.
    /// </summary>
    public static AktavaraTypeKind ParseTypeKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return AktavaraTypeKind.None;

        return Enum.TryParse<AktavaraTypeKind>(value, ignoreCase: true, out var result)
            ? result
            : AktavaraTypeKind.None;
    }

    /// <summary>
    /// Converts an integer TypeKind value to the enumeration.
    /// </summary>
    public static AktavaraTypeKind ParseTypeKind(int value)
    {
        return Enum.IsDefined(typeof(AktavaraTypeKind), value)
            ? (AktavaraTypeKind)value
            : AktavaraTypeKind.None;
    }

    /// <summary>
    /// Checks if a request type name matches a workspace request pattern.
    /// </summary>
    public static bool IsWorkspaceRequest(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        return typeName.Contains("GetPathWorkspaceData") ||
               typeName.Contains("GetPathWsData") ||
               typeName.Contains("GetWsTopology") ||
               typeName.Contains("GetTopologyData") ||
               typeName.Contains("GetWsDiagram") ||
               typeName.Contains("GetDiagramData") ||
               typeName.Contains("GetWsCarrier") ||
               typeName.Contains("GetWsSchema") ||
               typeName.Contains("GetBranchTopology");
    }

    /// <summary>
    /// Checks if a request type name matches a data query request pattern.
    /// </summary>
    public static bool IsDataQueryRequest(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        return typeName.Contains("GetEntities") ||
               typeName.Contains("GetOptions") ||
               typeName.Contains("GetTypesByTypeKind") ||
               typeName.Contains("GetRelationships");
    }

    /// <summary>
    /// Gets the workspace type for a given TypeKind.
    /// Multiple TypeKinds may map to the same workspace type.
    /// </summary>
    public static AktavaraWorkspaceType GetWorkspaceType(AktavaraTypeKind typeKind)
    {
        return typeKind switch
        {
            AktavaraTypeKind.Path or AktavaraTypeKind.BranchPath => AktavaraWorkspaceType.Path,
            AktavaraTypeKind.Topology or AktavaraTypeKind.BranchTopology => AktavaraWorkspaceType.Topology,
            AktavaraTypeKind.Diagram => AktavaraWorkspaceType.Diagram,
            AktavaraTypeKind.Carrier => AktavaraWorkspaceType.Carrier,
            AktavaraTypeKind.Schema => AktavaraWorkspaceType.Schema,
            AktavaraTypeKind.Collection => AktavaraWorkspaceType.Collection,
            _ => AktavaraWorkspaceType.Default
        };
    }

    /// <summary>
    /// Determines if a TypeKind represents a primary entity (vs. a structural element).
    /// Primary entities are: Node, Connector, Path, Topology, Diagram, Carrier, Schema.
    /// </summary>
    public static bool IsPrimaryEntity(AktavaraTypeKind typeKind)
    {
        return typeKind switch
        {
            AktavaraTypeKind.Node => true,
            AktavaraTypeKind.Connector => true,
            AktavaraTypeKind.Path => true,
            AktavaraTypeKind.Topology => true,
            AktavaraTypeKind.Diagram => true,
            AktavaraTypeKind.Carrier => true,
            AktavaraTypeKind.Schema => true,
            AktavaraTypeKind.BranchNode => true,
            AktavaraTypeKind.BranchConnector => true,
            AktavaraTypeKind.BranchPath => true,
            AktavaraTypeKind.BranchTopology => true,
            _ => false
        };
    }

    /// <summary>
    /// Converts an AktavaraTypeKind to a simplified RecordKind for domain model purposes.
    /// Maps all related types (e.g., BranchNode -> Node).
    /// </summary>
    public static RecordKind ToRecordKind(AktavaraTypeKind typeKind)
    {
        return typeKind switch
        {
            AktavaraTypeKind.Node or AktavaraTypeKind.BranchNode => RecordKind.Node,
            AktavaraTypeKind.Connector or AktavaraTypeKind.BranchConnector => RecordKind.Connector,
            AktavaraTypeKind.Path or AktavaraTypeKind.BranchPath => RecordKind.Path,
            _ => RecordKind.Other
        };
    }

    /// <summary>
    /// Converts a string representation to a RecordKind.
    /// Uses the new authoritative enum system.
    /// </summary>
    public static RecordKind ToRecordKind(string? value)
    {
        var typeKind = ParseTypeKind(value);
        return ToRecordKind(typeKind);
    }
}
