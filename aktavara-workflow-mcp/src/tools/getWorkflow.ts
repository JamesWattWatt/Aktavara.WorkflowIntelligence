import { z } from 'zod';
import * as api from '../apiClient.js';

export const getWorkflowSchema = z.object({
  workflowId: z.string()
    .describe('The workflow ID (e.g., "update-node-in-path")')
});

export type GetWorkflowInput = z.infer<typeof getWorkflowSchema>;

export async function getWorkflow(input: GetWorkflowInput): Promise<string> {
  const workflow = await api.getWorkflow(input.workflowId);

  const lines: string[] = [];

  lines.push(`# ${workflow.name}`);
  lines.push('');

  if (workflow.description) {
    lines.push(workflow.description);
    lines.push('');
  }

  lines.push('## Overview');
  lines.push(`- **Workflow ID:** \`${workflow.workflowId}\``);
  lines.push(`- **Tags:** ${workflow.tags.join(', ') || 'None'}`);
  lines.push('');

  // Activity signature
  if (workflow.activitySignature && workflow.activitySignature.length > 0) {
    lines.push('## Activity Signature');
    lines.push(
      '(The types of events that indicate this workflow is in progress)'
    );
    lines.push('');

    workflow.activitySignature.forEach((rule, idx) => {
      lines.push(`${idx + 1}. **${rule.eventType}**`);
      lines.push(`   - Description: ${rule.description}`);
      lines.push(`   - Weight: ${rule.weight}`);
      lines.push(`   - Required: ${rule.required ? 'Yes' : 'No'}`);
      lines.push(`   - Missing Penalty: ${rule.missingPenalty}`);
    });
    lines.push('');
  }

  // States and transitions
  if (workflow.states && workflow.states.length > 0) {
    lines.push('## Workflow States');
    lines.push('');

    workflow.states.forEach((state) => {
      const terminal = state.isTerminal ? ' [TERMINAL]' : '';
      lines.push(`### ${state.sequence + 1}. ${state.name}${terminal}`);
      lines.push(`- **State ID:** \`${state.stateId}\``);

      if (state.requiredEvidence && state.requiredEvidence.length > 0) {
        lines.push(`- **Requires:** ${state.requiredEvidence.join(', ')}`);
      }

      if (state.nextStateId && !state.isTerminal) {
        lines.push(`- **Next State:** ${state.nextStateId}`);
      }

      if (state.helpGuideId) {
        lines.push(`- **Help Guide:** ${state.helpGuideId}`);
      }

      lines.push('');
    });
  }

  return lines.join('\n');
}
