import { z } from 'zod';
import * as api from '../apiClient.js';
export const analyzeActivitySchema = z.object({
    logContent: z.string().describe('Raw activity log text to analyze'),
    userName: z.string().describe('The Aktavara user name'),
    timeWindowMinutes: z.number().optional().default(30)
        .describe('Time window in minutes to consider (default 30)'),
    userQuestion: z.string().optional()
        .describe('Optional: what the user says they are trying to do')
});
export async function analyzeActivity(input) {
    const response = await api.analyzeText(input.logContent, input.userName, input.timeWindowMinutes, input.userQuestion);
    // Format as structured markdown for LLM consumption
    const lines = [];
    lines.push('## Analysis Results');
    lines.push('');
    // Current state and active entities
    lines.push('### Current State');
    lines.push(`**State:** ${response.currentState}`);
    if (response.activeEntities.length > 0) {
        lines.push('**Active Entities:**');
        response.activeEntities.forEach(entity => {
            lines.push(`  - ${entity.recordKind}: ${entity.name} (ID: ${entity.recordId})`);
        });
    }
    lines.push('');
    // Top workflow match
    if (response.workflowCandidates.length > 0) {
        const topMatch = response.workflowCandidates[0];
        lines.push('### Detected Workflow');
        lines.push(`**Name:** ${topMatch.workflowName}`);
        lines.push(`**Confidence:** ${(topMatch.confidenceScore * 100).toFixed(0)}% (${topMatch.confidenceLevel})`);
        if (topMatch.matchedEvidence.length > 0) {
            lines.push('**Evidence:**');
            topMatch.matchedEvidence.slice(0, 3).forEach(ev => {
                lines.push(`  - ${ev}`);
            });
            if (topMatch.matchedEvidence.length > 3) {
                lines.push(`  - ... and ${topMatch.matchedEvidence.length - 3} more events`);
            }
        }
        lines.push('');
    }
    // Guidance and next step
    lines.push('### Guidance');
    lines.push(`**Level:** ${response.guidanceLevel}`);
    if (response.recommendedNextStep) {
        lines.push(`**Next Step:** ${response.recommendedNextStep}`);
    }
    lines.push('');
    // Semantic matches if user provided a question
    if (response.semanticMatches.length > 0) {
        lines.push('### Semantic Matches (from your question)');
        response.semanticMatches.slice(0, 3).forEach(match => {
            lines.push(`**${match.workflowName}** (${(match.score * 100).toFixed(0)}%)`);
            lines.push(`  ${match.reason}`);
        });
        lines.push('');
    }
    // Ambiguity signal
    if (response.ambiguity?.isAmbiguous) {
        lines.push('### Ambiguity Detected');
        lines.push(`**Recommended Action:** ${response.ambiguity.recommendedAction}`);
        if (response.ambiguity.clarificationQuestion) {
            lines.push(`**Clarification Needed:** ${response.ambiguity.clarificationQuestion}`);
        }
        lines.push('');
    }
    // Context narrative
    lines.push('### Context');
    lines.push(response.contextNarrative);
    return lines.join('\n');
}
//# sourceMappingURL=analyzeActivity.js.map