# Planning Workflow

The **Planning Workflow** feature separates planning and operational inventory data into two distinct database schemas:

- **Planning schema** — for draft and design work.
- **Operational schema** — for live, production data.

The workflow allows planned changes to be collected in a worksheet and later synchronized with the operational schema. This feature has not been widely used, so before beginning on an implementation project we recommend contacting Aktavara Support. 

---

## Overview

### Key Concepts
- **Worksheet Trace** — a rule-based system (configured in Aktavara Designer) that automatically collects changes made in the planning schema into a worksheet.  
- **Synchronization** — compares and updates operational data to match the planning worksheet.

### Workflow Steps
1. **Start a Worksheet Trace** to collect planned changes in a worksheet.  
   - Planning worksheets include a **Status** field (default: *In Progress*).
2. **Make changes** in the planning schema.  
   - Changes are automatically recorded to the worksheet (or can be added manually).  
   - When complete, set the worksheet status to **Planning Completed**.
3. **Synchronize** with the operational schema.  
   - The synchronizer updates operational data to match the planning worksheet.  
   - Upon success, worksheet status becomes **Operational** (set automatically).

---

## Starting a Worksheet Trace

1. Start **Aktavara Console**.  
2. If not visible, open **View → Worksheet Trace**.  
3. The *Worksheet Traces* window appears.  
4. Hover over the desired trace → a list of worksheets appears.  
5. Select a worksheet to record upcoming changes.  
   - The active trace is shown with a **green background**.  
6. To start a new worksheet, choose **Trace in New Worksheet**.

All changes made to planning data are now recorded automatically.

---

## Recording Changes & Setting Worksheet Status

1. Start **Aktavara Console**.  
2. If a worksheet trace is active, all qualifying changes (via workspace, property sheet, or spreadsheet) are automatically recorded — based on rules set in **Aktavara Designer**.  
3. To stop tracing: right‑click the trace → **Stop Tracing**.  
4. Open **Tools → Worksheets** to view collected data.  
5. In the *Status* column, change from *In Progress* to **Planning Completed** when ready for synchronization.

---

## Synchronizing Planning and Operational Schemas

When planning is finished and physical changes are complete, use the **Worksheet Synchronizer** to update the operational schema.

1. Start **Aktavara Console**.  
2. Open **Tools → Worksheets**.  
3. Right‑click the worksheet → select **Synchronize**.  
4. The synchronizer compares the planning schema based worksheet to the operational data and updates it so both schemas are identical.  
5. After completion:  
   - The synchronized worksheet opens with records grouped by type.  
   - A **completion report** appears in the Messages window.  
   - Worksheet status updates to **Operational** automatically.

> 🗒️ Synchronization can also be initiated from an associated record to synchronize all related worksheets.

---

## Planning Workflow Options

You can control trace and synchronization behavior in **User Options**.

### User Options
1. Start **Aktavara Console** → open **Side Bar Info Sheet → Options**.  
2. The *User Options* window appears.  
3. Configure planning workflow settings:
   - **Trace Login Reminder:** Show last active trace and worksheet upon login.  
   - **Trace on Last Active Configuration:** Resume last active trace automatically after login.  
   - **Trace Save Reminder:** Warn if saving data while trace is inactive.  
4. Click **OK** to save settings.

### Worksheet‑Level Options
1. Open **Tools → Worksheets**.  
2. Right‑click a worksheet → **Options**.  
3. Enable **View Deleted Records** to see deleted entries in a “Deleted Records” group (displayed with strikethrough).  
   - This helps preview what will be removed during synchronization.  
4. Click **OK** to confirm.

---

✅ **Tip:** Use worksheet traces to automatically log planning changes and maintain full traceability from design to operations.
