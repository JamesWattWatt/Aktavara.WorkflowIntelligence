import { z } from 'zod';
import * as api from '../apiClient.js';

export const detectTaskSchema = z.object({
  logContent: z.string().describe('Raw activity log text'),
  userName: z.string().describe('The Aktavara user name'),
  userQuestion: z.string().optional()
    .describe('Optional: what the user says they are trying to do'),
  timeWindowMinutes: z.number().optional().default(30)
    .describe('Time window in minutes (default 30)')
});

export type DetectTaskInput = z.infer<typeof detectTaskSchema>;

export async function detectTask(input: DetectTaskInput): Promise<string> {
  const response = await api.analyzeText(
    input.logContent,
    input.userName,
    input.timeWindowMinutes,
    input.userQuestion
  );

  // Return structured JSON object
  const result = {
    currentState: response.currentState,
    detectedWorkflow: response.workflowCandidates.length > 0
      ? {
          id: response.workflowCandidates[0].workflowId,
          name: response.workflowCandidates[0].workflowName,
          confidence: response.workflowCandidates[0].confidenceScore,
          level: response.workflowCandidates[0].confidenceLevel
        }
      : null,
    nextStep: response.recommendedNextStep || null,
    activeEntities: response.activeEntities.map(e => ({
      kind: e.recordKind,
      name: e.name,
      id: e.recordId
    })),
    ambiguity: response.ambiguity ? {
      isAmbiguous: response.ambiguity.isAmbiguous,
      recommendedAction: response.ambiguity.recommendedAction,
      clarificationQuestion: response.ambiguity.clarificationQuestion || null
    } : null,
    semanticMatches: response.semanticMatches.length > 0
      ? response.semanticMatches.slice(0, 3).map(m => ({
          workflowId: m.workflowId,
          workflowName: m.workflowName,
          score: m.score
        }))
      : []
  };

  return JSON.stringify(result, null, 2);
}
