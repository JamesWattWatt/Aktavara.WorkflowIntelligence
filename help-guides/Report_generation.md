# Report Generation

The **Report Generation** feature allows users to create, manage, and monitor reports directly within the **Aktavara Console**.

---

## Starting a Report Generation

A report can be generated from the **context menu** of a record via the **Reports** option.  Reports are organized by **type** and optional **category**.

- Ongoing report generations are displayed in the **Progress** workspace.  
- Results are shown in the **Messages** workspace once generation is complete.

---

## Report-Related Options

Three configuration options affect how reports are generated and handled. These are found under **Console Workspace Options**.

### Option Descriptions

| Option | Description |
|--------|--------------|
| **Open report automatically** | If set to *True*, the Console opens the generated report file automatically using the associated Windows application. |
| **Report status refresh (sec)** | Defines how often (in seconds) the report status updates in the *Report Manager* workspace. |
| **Show saving dialog** | If set to *True*, a dialog appears before generation starts, allowing users to edit report properties (name, save location, and options). |

> 💡 When configured with *Open report automatically = True*, reports open as soon as generation finishes.

---

## Report Manager

The **Report Manager** workspace lists all reports — generated, in progress, or queued for later generation.

### Opening the Report Manager
- Toolbar **View → Report Manager** on the toolbar.

### Report Manager Actions

Right-click a report in the workspace to perform additional actions:

| Action | Description |
|--------|--------------|
| **Generate Reports / Stop Generating Reports** | Starts or pauses the report generation queue. Useful when adding multiple reports before processing them together. |
| **Clear Completed and Erroneous Reports** | Removes all reports that have finished or failed. |
| **Cancel Report Generation** | Cancels an active report generation. |
| **Move Report Up / Move Report Down** | Changes the order of reports waiting to be generated. |
| **Open Report** | Opens a completed report (also available via double-click). |
| **Open Log File** | Opens the log file for a report (available for completed or failed reports). Logs provide details about errors. |
| **Remove Report** | Deletes the selected report entry from the list. |

---

✅ **Tip:** Keep the **Report Manager** open to monitor progress, quickly access generated reports, and review logs for troubleshooting.
