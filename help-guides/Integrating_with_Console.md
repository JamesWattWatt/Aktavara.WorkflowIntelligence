# Integrating with the Console

It is possible to integrate with the Console using special URIs (Uniform Resource Identifiers). These links can be embedded in web pages or documents and provide a way to directly display specific data or records in the Console.

---

## Overview

When a valid **Console link** is clicked:
- If the Console is **already open** and connected to the schema defined in the link, it will come to the foreground and display the specified records or search results.
- If the Console is **not open** or connected to a different schema, a new Console instance is launched. The user may need to log in before the data is displayed.

> The interaction is **local to the computer** — links will only activate if the Console is installed and configured locally.

---

## General Format

```
akta:schema={SCHEMA_NAME}&Action={Workspace|Explorer|Spreadsheet|Search}&RECORD_ID={OBJECT_ID | comma-separated list}&TYPE_NAME={TYPE_NAME | comma-separated list}
```

### Parameters

| Parameter | Description |
|------------|--------------|
| **schema** | The schema name as shown in the Console login window (i.e., the schema alias). |
| **action** | The operation to perform in the Console: open a workspace, run a search, or show records. |
| **record_id** | One or more object IDs (comma-separated) identifying the records to display. |
| **type_name** | One or more type names or IDs (comma-separated). Must match the number of entries in `record_id`. |

### Common Actions

| Action | Description |
|--------|--------------|
| **Explorer** | Opens the specified records in the Explorer view. |
| **Workspace** | Opens a record in its workspace or graphical editor (e.g., path or topology). |
| **Spreadsheet** | Displays one or multiple records in spreadsheet view. |
| **Search** | Executes a custom search starting from the specified record. |

---

## Examples

| Example URI | Description |
|--------------|-------------|
| `akta:schema=COMMUNICATOR_DEMO&Action=Explorer&RECORD_ID=1&TYPE_NAME=NETWORK` | Opens a Network record in the Explorer. |
| `akta:schema=COMMUNICATOR_DEMO&Action=Workspace&RECORD_ID=1467&TYPE_NAME=NE` | Opens a Network Element (NE) record in a graphics workspace. |
| `akta:schema=COMMUNICATOR_DEMO&Action=Workspace&RECORD_ID=108&TYPE_NAME=DOMAIN1` | Opens a DOMAIN1 Topology workspace. |
| `akta:schema=COMMUNICATOR_DEMO&Action=Explorer&RECORD_ID=111111111&TYPE_NAME=DOESNOTEXIST` | Displays an error message for an invalid record. |
| `akta:schema=COMMUNICATOR_DEMO&Action=Spreadsheet&RECORD_ID=1470,109&TYPE_NAME=NE,DOMAIN1` | Opens multiple records in a spreadsheet using type names. |
| `akta:schema=COMMUNICATOR_DEMO&Action=Explorer&RECORD_ID=405,406,407&TYPE_NAME=13,13,13` | Opens multiple records in the Explorer using type IDs. |
| `akta:schema=COMMUNICATOR_DEMO&Action=Search&ID=10&RECORD_ID=1468&TYPE_NAME=NE` | Runs a custom search for a specific NE record. |

---

## Notes
- The **schema name** and **record identifiers** must exactly match the configuration in your Console environment.
- Links using invalid records or types will display an error message in the Console’s **Messages** window.
- These links are ideal for embedding in documentation, reports, or dashboards that require direct access to Console data.

