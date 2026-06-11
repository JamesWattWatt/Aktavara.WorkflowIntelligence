import { z } from 'zod';
import * as api from '../apiClient.js';
export const explainNextStepSchema = z.object({
    logContent: z.string().describe('Raw activity log text'),
    userName: z.string().describe('The Aktavara user name'),
    userQuestion: z.string().optional()
        .describe('Optional: what the user says they are trying to do'),
    timeWindowMinutes: z.number().optional().default(30)
        .describe('Time window in minutes (default 30)')
});
export async function explainNextStep(input) {
    const response = await api.analyzeText(input.logContent, input.userName, input.timeWindowMinutes, input.userQuestion);
    const lines = [];
    // Current Situation
    lines.push('## Current Situation');
    lines.push('');
    lines.push(`**State:** ${response.currentState}`);
    if (response.activeEntities.length > 0) {
        lines.push('**Working with:**');
        response.activeEntities.slice(0, 5).forEach(entity => {
            lines.push(`  - ${entity.recordKind}: ${entity.name}`);
        });
    }
    lines.push('');
    // Detected Task
    if (response.workflowCandidates.length > 0) {
        const topMatch = response.workflowCandidates[0];
        lines.push('## Detected Task');
        lines.push('');
        lines.push(`**Workflow:** ${topMatch.workflowName}`);
        lines.push(`**Confidence:** ${(topMatch.confidenceScore * 100).toFixed(0)}% (${topMatch.confidenceLevel})`);
        lines.push('');
        // Get workflow definition for more context
        try {
            const workflow = await api.getWorkflow(topMatch.workflowId);
            if (workflow.description) {
                lines.push(`**About:** ${workflow.description}`);
                lines.push('');
            }
        }
        catch {
            // Continue if we can't fetch workflow details
        }
    }
    else {
        lines.push('## No Clear Task Detected');
        lines.push('');
        lines.push('The activity does not clearly match any known workflow pattern.');
        lines.push('');
    }
    // Recommended Next Step
    lines.push('## Recommended Next Step');
    lines.push('');
    if (response.recommendedNextStep) {
        lines.push(`**Action:** ${response.recommendedNextStep}`);
        lines.push('');
    }
    else {
        lines.push('Continue with your current workflow.');
        lines.push('');
    }
    // Relevant Documentation
    if (response.workflowCandidates.length > 0) {
        const topMatch = response.workflowCandidates[0];
        try {
            const sections = await api.getHelpGuideSection(topMatch.workflowId, topMatch.currentStateName || '');
            if (sections.length > 0) {
                lines.push('## Relevant Documentation');
                lines.push('');
                sections.slice(0, 2).forEach(section => {
                    lines.push(`### ${section.heading}`);
                    lines.push('');
                    lines.push(section.content.substring(0, 500));
                    if (section.content.length > 500) {
                        lines.push('...');
                    }
                    lines.push('');
                });
            }
        }
        catch {
            // Continue if help guides unavailable
        }
    }
    // Clarification Needed
    if (response.ambiguity?.isAmbiguous && response.ambiguity.clarificationQuestion) {
        lines.push('## Clarification Needed');
        lines.push('');
        lines.push(`**Question:** ${response.ambiguity.clarificationQuestion}`);
        lines.push('');
    }
    return lines.join('\n');
}
//# sourceMappingURL=explainNextStep.js.map