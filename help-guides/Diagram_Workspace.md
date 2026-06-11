# Diagram Workspace

The **Diagram Workspace** provides a powerful and flexible environment for creating visual representations of network connectivity. It allows users to view both **logical** and **physical relationships** between network components, offering a high-level overview that can be customized to suit various analysis or documentation needs.

Diagrams can show relationships as **arrows**, **containment boxes**, or other shapes, depending on **design-time settings**. Colors, line layouts, and relationship types are configured per diagram type, ensuring consistency and readability across the organization.

Additionally, the workspace supports **auto-generation of hierarchical diagrams**, allowing multiple layers of connectivity (e.g., logical, physical, patch) to be visualized automatically.

---

## Starting the Diagram Workspace

To open the Diagram Workspace:

1. Launch **Aktavara Console**.  
2. In the **Explorer Workspace**, expand **Diagrams**, locate the diagram type, and double-click the desired record.  
3. Alternatively, double-click a diagram entry in the **Spreadsheet Workspace**.

The selected diagram opens in the workspace, showing all defined nodes and relationships.

---

## Adding Nodes to a Diagram

Nodes can be manually added to visualize their relationships and connectivity.

### Add a Node

1. In the **Explorer Workspace**, locate the desired node.  
2. Drag and drop it into the **Diagram Workspace**.  
3. The node appears in the diagram.

### Add Parent Nodes

1. Select a node in the diagram.  
2. Right-click and choose **Add Parent**.  
3. In the **Add Parents** dialog, use **Parent Level** to choose how many hierarchy levels to include, or select **All** to include every parent.  
4. Click **OK** to add the parent nodes.

### Add Child Nodes

1. Select a node in the diagram.  
2. Right-click and choose **Add Children**.  
3. In the **Check for Relationships** dialog:  
   - Choose the **Type** of relationship (e.g., connector, path).  
   - Use filters to refine which child nodes appear.  
   - Select desired elements and click **>** or **>>** to add them.  
4. Click **OK** to confirm and display the child nodes.

---

## Diagram Auto Generation

The **Auto-Generation** feature automatically creates nested diagrams across multiple connectivity layers.

### To Generate Diagram Hierarchies

1. Create a new diagram (starting from the topmost layer, e.g., *Layer 5*).  
2. Define input nodes or path endpoints.  
3. Validate connectivity between endpoints.  
4. Save the diagram and initiate **Auto Generate**.  
5. Generated diagrams appear hierarchically in a **Spreadsheet View**, with each layer below linked to the one above (e.g., Layer 5 → Layer 4 → Layer 3).  
6. Generated diagrams are temporary; save them manually if needed.

---

## Removing Nodes

1. Locate the node in the workspace.  
2. Press **DEL** or right-click and select **Remove**.  
3. The node is deleted from the diagram.

---

## Opening Nodes in Related Workspaces

To explore details of a node:

1. Right-click a node and select **Open → Graphics Workspace** or **Open → Spreadsheet**.  
2. The node opens in the corresponding workspace.

---

## Locating Nodes in the Explorer

1. Select a node in the diagram.  
2. Hover and click **Show in Explorer** from the mini toolbar.  
3. The **Explorer Workspace** expands to highlight the selected node.

---

## Checking for Relationships

The **Check for Relationships** feature is a powerful tool used to analyze and visualize how elements in a diagram are connected. It can reveal structural relationships—such as parent/child hierarchies, connector links, or path-based connections—and draw them directly on the diagram to help users better understand network structure.

This tool is especially useful when diagrams contain many elements and you need a clear, automated way to highlight how selected nodes relate to each other.

---

### How to Add Relationships to a Diagram

1. Open the Diagram Workspace. Open or create a diagram in the **Diagram workspace**.
2. Start the Relationship Checker. Click **Check for Relationships** in the workspace toolbar. This opens the **Check for Relationships** dialog.

---

### Configuring the Relationship Search

Within the dialog, you can define:

- **What types of relationships to look for**  
- **Which elements of the diagram to include**  
- **How detailed or simplified the resulting relationship drawing should be**

Each category of relationships contains additional filtering and control options.

---

### Parent / Child Relationships

Enable these to detect hierarchical relationships.

- **Direct** – Finds immediate parent-child connections  
- **Indirect** – Includes deeper hierarchical ancestry (parents of parents, children of children, etc.)

These relationships are drawn between the nodes you select.

---

### Connector Relationships

Detects whether selected nodes are linked by connectors.

**Options:**

- **Direct / Indirect** – Whether connections are immediate or through intermediate nodes  
- **Connector Type Filtering**  
  - Expand the **Types** dropdown  
  - Use **Filter by** to narrow connector types  
  - Choose specific connector types using **CTRL-click / SHIFT-click**, then click **Add**  
  - To remove types:  
    - Switch **Filter by → <All selected>**  
    - Select types and click **Remove**

---

#### Path Relationships

Identifies path-based relationships between nodes.

**Options:**

- **Direct / Indirect**  
- **Path Type Filtering**  
  - Use **Filter by** to select or narrow path types  
  - Multi-select and **Add** desired types  
  - Remove types via **<All selected> → Remove**
- **End Points Only**  
  Restricts search to paths where selected nodes (or their parents) act as endpoints.

---

### Selecting Diagram Elements

Specify which diagram elements should be included in the search.

**Steps:**

1. Select items from **All elements** (filter if necessary).  
2. Move them into the **Selected elements** list using:  
   - **>** — add selected  
   - **>>** — add all  
   - **<** — remove selected  
   - **<<** — remove all  

Only items in **Selected elements** are considered for relationship detection.

---

### Removing Redundant Relations

Enable **Remove redundant relations** to avoid clutter by suppressing indirect or unnecessary connections.

Example:  
A parent node may be indirectly related through multiple levels, but this option prevents drawing redundant lines, keeping the diagram clear.

---

## Executing the Search

1. Click **OK** to execute the relationship search.  
2. All detected relationships are drawn directly onto the diagram based on your settings.

---

## Diagram Cleanup

To simplify a cluttered diagram:

1. Click **Diagram Cleanup** from the toolbar.  
2. In the dialog, check elements or relationship types to remove.  
3. Click **OK** to apply cleanup.

---

## Temporarily Hiding Relationships

To filter visibility of certain connections:

1. Right-click an empty area and choose **Show Relations**.  
2. Use the submenu to toggle visibility for specific relationship types.

---

## Showing Relationships in Explorer

1. Hover or right-click a relationship line.  
2. Choose **Show in Explorer** from the context menu.  
3. A tabular view of the relationship appears in a dialog box.

---

## Finding Items in a Diagram

1. Use the **Search** dropdown in the toolbar.  
2. Type partial or full names to filter items.  
3. Select an item from the list — it will be highlighted in the diagram.

---

## Working with Notes

### Adding Notes

1. Right-click an empty area and select **Notes → Add Note** (or click **Add Note** on the toolbar).  
2. A note appears — double-click to edit its content.

### Formatting Notes

- Right-click a note and choose **Format**.  
- Adjust **Font Style**, **Font Size**, or **Text Alignment** as needed.

### Hiding and Removing Notes

- Hide a single note via **Hide** from its context menu.  
- Hide all notes via **Notes → Hide All Notes**.  
- Remove a note with **DEL** or **Remove Note**.

---

## Switching to Spreadsheet View

1. Right-click an empty area and choose **Spreadsheet View**.  
2. A tabular list of all diagram elements opens for analysis.

---

## Changing the Diagram View

Depending on Designer configuration, diagrams can display **Full Connectivity** or **Logical Connectivity**.

### View Options

- **Full Connectivity View:** Shows all physical and logical links as defined by the diagram creator.  
- **Logical Connectivity View:** Focuses on logical relationships, hiding sub-nodes and internal connections.  
  
  > ⚠️ If no logical entities are defined, the logical view mirrors full connectivity.

### Extended Logical View Options

- **Show Interfaces:** Adds interface nodes to logical nodes.  
- **Show Internal Connectivity:** Displays hidden internal connections.  
- **Show Parent Nodes:** Toggles parent entity visibility.

> 🔧 **Note:** Type-specific icons are configured in Designer.

---

## Legend

To view diagram representation keys:

1. Click **Legend** on the toolbar.  
2. The **Legend Dialog** appears, showing line styles, colors, and relationship indicators per diagram type.

---

## Exporting a Diagram as an Image

1. Click **Export** from the toolbar.  
2. In the **Image Exporter** dialog, define **Export Bounds** and **Image Options** (format, size).  
3. Click **Save To File**, name the file, and choose a folder.  
4. The diagram image is saved for documentation or sharing.

---


