export const helpContent: Record<string, { title: string; content: string }> = {
  'discovery-concept': {
    title: 'About Workflow Discovery',
    content: `## What is Workflow Discovery?

Workflow Discovery analyses your Aktavara activity logs to automatically detect patterns in how users work.

By uploading a log file, the system identifies sequences of actions — searching for records, opening workspaces, saving changes — and matches them against known workflow patterns.

### How it works
1. Upload an activity log file (.txt format)
2. The system parses and normalises the events
3. Workflow patterns are matched with confidence scores
4. The detected workflow appears in the candidate list

### Confidence levels
- **High (≥85%)** — Pattern clearly identified, guidance available
- **Medium (55-84%)** — Pattern likely, confirmation recommended
- **Low (<55%)** — Pattern unclear, manual review needed

### What you can do
Once a workflow is detected, use the **Workshop** tab to qualify it, capture customer context, and approve it for the guidance library.`
  },
  'discovery-analysis': {
    title: 'Understanding the Analysis Panel',
    content: `## Analysis Summary

After uploading a log file, the Analysis panel shows a summary of what was found.

### Key metrics
- **Events** — Total activity events detected in the log
- **Candidates** — Number of workflow patterns matched
- **Time** — How long the analysis took
- **Guidance level** — How confidently the system can guide the user:
  - *Instruct* — High confidence, direct guidance available
  - *Confirm* — Medium confidence, user confirmation recommended
  - *Suggest* — Low confidence, suggestion only
  - *No Guidance* — No clear pattern detected

### Context narrative
The text below the metrics is a plain-English summary of what the system observed — who was working, what records they touched, and what workflow was detected.

### Next step
The recommended next step links to the help guide section most relevant to the current workflow state.`
  },
  'discovery-workflow-details': {
    title: 'Workflow Details',
    content: `## Workflow Details Panel

This panel shows the full analysis of the selected workflow candidate.

### Confidence score
The percentage reflects how closely the observed activity matched the workflow's signature rules.

### Score breakdown
- **Matched Rules** — Weight from rules that fired
- **Missing Penalty** — Deduction for required rules not found
- **Sequence Bonus** — Bonus when steps occurred in the expected order
- **Entity Correlation** — Bonus when related records appear together
- **Staleness Penalty** — Deduction for old events outside the time window

### Matched evidence
The green tags show specific events from the log that triggered each rule — including record IDs and timestamps.

### Matched rules
The checklist shows which workflow steps were observed. Required steps must be present for a High confidence score.

### Next step hint
The amber box shows the recommended next action based on the current workflow state and your help guide mappings.`
  },
  'discovery-workshop': {
    title: 'Workshop Qualification',
    content: `## Workshop Panel

The Workshop panel is used during customer workshops to qualify detected workflows and build the guidance library.

### Workflow name
The detected name is a technical label. Replace it with the name your customers actually use for this task.

### Workshop questions
These questions are pre-generated based on the detected workflow pattern. Use them to:
- Confirm the workflow matches what the customer does
- Capture edge cases and variations
- Document business rules and validation steps

Check each question as you discuss it and add notes capturing the customer's answers.

### Execution decision
Choose how this workflow should be supported:
- **Guidance only** — The assistant explains what to do, the user does it manually. Lowest risk, easiest to implement.
- **Assisted action** — The assistant performs individual steps with the user's explicit approval at each step.
- **Governed execution** — The full workflow runs automatically with pre-approved rules. Requires additional implementation.

### Approval
- **Approve** — Adds this workflow to the active guidance library
- **Candidate** — Keeps it as a detected pattern pending review
- **Deprecate** — Removes it from active use

### Export session
Downloads a JSON file with the full workshop session — customer name, answered questions, notes, and decision. Use this to build your workflow documentation.`
  },
  'discovery-workshop-guide': {
    title: 'Relevant Documentation',
    content: `## Workflow Step Guides

The Relevant Documentation section displays help content mapped to the current workflow state.

### What appears here
When a help guide is mapped for the current step, its content appears in this section. This might include:
- Step-by-step instructions for the user
- Business rules or validation requirements
- Common pitfalls and how to avoid them
- Links to external documentation

### Suggested guides
If no guide is currently mapped, the system can suggest one based on the matched rules and evidence:
- Click **"✨ Suggest Guide"** to find relevant documentation
- Review the suggestion (file and section)
- Click **"✓ Approve"** to add the mapping
- Click **"✗ Dismiss"** to skip it

### Editing mappings
To change the guide mapped to this step, go to the **Library** tab, select the workflow, open the **Guides** tab, and adjust the mapping there.

### Why this matters
Good guide mappings help users understand what they're doing and why, reducing errors and support requests.`
  },
  'library-concept': {
    title: 'About the Workflow Library',
    content: `## Workflow Library

The Library is where all workflow definitions are managed. Each workflow definition tells the system what activity patterns to look for and how to guide users through them.

### Workflow statuses
- **Approved** — Active in the guidance system, shown to users
- **Candidate** — Detected or created but not yet approved
- **Deprecated** — No longer active, kept for reference

### Workflow definitions
Each workflow contains:
- **Signature rules** — Activity patterns that identify the workflow
- **State model** — The steps a user progresses through
- **Help guide mappings** — Which documentation applies at each step
- **Workshop questions** — Questions for customer qualification sessions

### Creating workflows
Workflows can be created three ways:
1. **Discovery** — Upload a log file and approve a detected candidate
2. **Inference** — Use the "Infer from logs" button to auto-generate a definition from log evidence
3. **Manual** — Click "New workflow" and define it from scratch

### Importing and exporting
Use Import/Export to share workflow definitions between installations or back them up for version control.`
  },
  'library-edit': {
    title: 'Editing a Workflow',
    content: `## Workflow Editor

The editor has five tabs for managing different aspects of a workflow definition.

### Overview tab
Set the basic properties:
- **Name** — What users call this task (use customer language)
- **Description** — One sentence explaining the workflow's purpose
- **Risk level** — Low (read-only), Medium (saves data), High (creates or deletes records)
- **Tags** — Record types involved (Path, Node, Connector etc.)
- **Status** — Approved, Candidate, or Deprecated
- **Confidence threshold** — Minimum score to trigger guidance

Use **"Infer from logs"** to auto-populate these fields from log evidence.

### Rules tab
Signature rules define what activity patterns identify this workflow. Each rule has:
- **Name** — Description of the step
- **Type** — Required (must be present), Supporting (expected), Optional (sometimes present)
- **Weight** — Relative importance (higher = more influence on score)
- **EventType / RecordKind** — The specific activity to match

Use **"Normalise weights"** to ensure weights sum to 1.0.

### States tab
States represent the user's progress through the workflow:
- Each state corresponds to a step the user has reached
- **Next step hint** links to the relevant help guide section
- Mark the final state as **Terminal**

### Guides tab
Map each workflow state to a section in the Aktavara documentation. When a user is in that state, the mapped guide section appears in the Workshop panel.

Click **"Change mapping"** to pick a different guide section.

### JSON tab
Shows the complete workflow definition as JSON. Use Download to save a copy or share with other installations.`
  },
  'library-state-terminal': {
    title: 'Terminal state',
    content: `## What is a terminal state?

A terminal state is the final step in a workflow — the point at which the task is considered complete.

### How it works
When a user reaches a terminal state, the system knows the workflow is finished and stops suggesting next steps. Instead it may show a completion message or prompt the user to start a new task.

### Example
In the 'Add connector to path' workflow:
- Path Opened — not terminal (more steps follow)
- Connector Created — not terminal (path needs saving)
- Path Saved — terminal (workflow complete)

### When to mark a state as terminal
Mark a state as terminal when:
- The user has completed all required actions
- There are no further steps in this workflow
- The system should stop providing next-step guidance

Only one state should typically be terminal, though complex workflows may have multiple terminal states for different completion paths.`
  }
};
