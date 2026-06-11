# Workspaces & Windows

This guide introduces the various workspaces available in **Aktavara Console**, explaining their purpose and how to access them.

---

## Network Explorer
Provides a **hierarchical view** of your network, including Nodes, Connectors, Paths, Topologies, Carriers, Collections, Diagrams, and Schemas.  
Supports standard Windows operations such as drag-and-drop, copy/paste, and Ctrl-drag to copy.

**To open:** Ribbon → **View > Explorer**

---

## Properties
Displays detailed, editable information for selected objects. Automatically synchronized with other workspaces.

- Edit attributes directly in the Properties panel.
- Fields may include text, dropdowns, dates, or numeric inputs.
- Changes activate the **Save** button in the window.

**To open:** Ribbon → **View > Properties** or press **F4**

---

## Progress
Lists all **asynchronous operations** being executed.  
Users can monitor or cancel running tasks.

**To open:** Ribbon → **View > Progress**

---

## Messages
Shows **errors, warnings, and notifications**.  
Double-clicking a message opens the associated record or workspace. Messages can also be cleared or copied.

**To open:** Ribbon → **View > Messages**

---

## Information (Info)
Displays **contextual information** about a selected object. May include links, runtime documentation, or configurable actions.

**To open:** Ribbon → **View > Information**

---

## Record Searches
Lists **predefined searches** based on the selected object.  
Searches are defined by the administrator and vary by type.

**To open:** Ribbon → **View > Record Searches**  
Execute a search by double-clicking or pressing **Execute**.

---

## Spreadsheet
Displays records in a **tabular format**. Enables editing, inserting, filtering, sorting, grouping, and exporting data.

- Paging is automatic for large datasets.
- Export to text, Excel, or reports includes all data (not just visible pages).

**To open:** From Search Portal → choose **Spreadsheet** as the output.

---

## Graphics
Provides a **graphical representation** of physical equipment or infrastructure.  
Supports hierarchical visualization, color rules, free shapes (maps/floor plans), and attribute-based styling.

**To open:** Double-click a node in Explorer or Spreadsheet.  
> Not all node types have graphics configured.

**Common shortcuts:**  
Ctrl+Shift+L (Save Offline), Ctrl+Shift+Z (Zoom), Ctrl+Shift+G (Grid), Ctrl+E (Export), Ctrl+Shift+N (Add Note), Ctrl+Shift+B (Insert Image), Ctrl+Shift+D (Display Size)

---

## Path
Displays **logical connectivity** between network objects.  
Shows hierarchy: ports → cards → equipment shelves → nodes → links/paths.

**To open:** Double-click a path in Explorer or Spreadsheet.

**Shortcuts:** Ctrl+L (Auto Layout), Ctrl+Shift+L (Save Offline), Ctrl+Shift+Z (Zoom), Ctrl+Shift+G (Grid), Ctrl+E (Export), Ctrl+Shift+N (Add Note)

---

## Topology
Shows **network-level connectivity** via connectors, paths, and topologies.

**To open:** Double-click a topology in Explorer or Spreadsheet.

**Shortcuts:** Ctrl+L (Auto Layout), Ctrl+Shift+L (Save Offline), Ctrl+Shift+Z (Zoom), Ctrl+Shift+G (Grid), Ctrl+E (Export), Ctrl+Shift+N (Add Note)

---

## Carrier
Displays **physical connectivity** (e.g., wires, cables, fibers, ducts).

**To open:** Double-click a carrier in Explorer or Spreadsheet.

**Shortcuts:** Ctrl+L (Auto Layout), Ctrl+Shift+L (Save Offline), Ctrl+Shift+Z (Zoom), Ctrl+Shift+G (Grid), Ctrl+E (Export), Ctrl+Shift+N (Add Note)

---

## Diagram
Used for **custom network diagrams** showing logical or physical relationships.

**To open:** Double-click a diagram in Explorer or Spreadsheet.

**Shortcuts:** Ctrl+Shift+Z (Zoom), Ctrl+Shift+G (Grid), Ctrl+E (Export), Ctrl+Shift+N (Add Note), Ctrl+Shift+R (Check Relationships), Ctrl+Shift+L (Legend)

---

## Connections
Used to **create connectors** between nodes by dragging endpoints from Explorer.  
Supports creating multiple connectors simultaneously.

**To open:** Top Menu → **Tools > Connections**

**Shortcuts:** Move, Add Connectors, Multipopulate, Remove, Connect

---

## Routing
Used to **create paths** between nodes.  
Endpoints are added by dragging nodes from Explorer.

**To open:** Top Menu → **Tools > Routing**

**Shortcuts:** Search Routes, Add Routes, Remove, Add Route, Toggle Display

---

## Search Portal
Centralized interface for all search functionality.

- Access advanced, custom, or template-based searches.
- Save and pin searches to menus or record searches.
- Execute quick searches directly from toolbars.

**To open:**  
- Click the arrow next to the search field → double-click a search.  
- Or use **Ctrl+F, Ctrl+N** (new search) / **Ctrl+F, Ctrl+V** (view searches).

---

## External Data Explorer
Displays **external adapter data** and its comparison with system data.  
Available only if the **Communicator** application is enabled.

**To open:** View via **External Data Explorer** workspace.

---

## Map Workspace
Displays GIS data in a **map view**, supporting zoom, pan, and selection.  
Can be opened from the **View toolbar** or a **custom search** returning GIS results.

**To open:** Ribbon → **View > Map**

---

## Report Manager
Manages report generation and status.

Functions include:
- Start/Stop report generation queue  
- Clear completed/erroneous reports  
- Cancel in-progress reports  
- Move reports up/down in the queue  
- Open reports or log files  
- Remove reports from the list  

**To open:** Ribbon → **View > Report Manager**

---
