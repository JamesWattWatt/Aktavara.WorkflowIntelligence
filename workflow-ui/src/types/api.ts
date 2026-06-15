export interface AnalyzeResponse {
  sessionId: string;
  fileName: string;
  parsedAt: string;
  totalEntries: number;
  totalEvents: number;
  durationMs: number;
  currentState: string;
  guidanceLevel: string;
  recommendedNextStep: string | null;
  contextNarrative: string;
  activeEntities: SerializableActiveEntity[];
  workflowHints: string[];
  workflowCandidates: WorkflowCandidateResult[];
  semanticMatches: SemanticWorkflowMatch[];
  ambiguity: AmbiguitySignal | null;
}

export interface WorkflowCandidateResult {
  workflowId: string;
  workflowName: string;
  confidenceScore: number;
  confidenceLevel: string;
  currentStateName: string | null;
  matchedRules: string[];
  matchedEvidence: string[];
  missingRules: string[];
  nextStepHint: string | null;
  scoreBreakdown: Record<string, number>;
  workshopQuestions: string[];
}

export interface WorkflowSummary {
  id: string;
  name: string;
  status: 'Approved' | 'Candidate' | 'Deprecated';
  version: string;
  description: string;
  riskLevel: string;
  tags: string[];
  isValid: boolean;
  ruleCount: number;
  stateCount: number;
  confidenceThreshold: number;
}

export interface WorkflowDefinition {
  workflowId: string;
  name: string;
  description: string;
  version: string;
  status: number;
  activitySignature: ActivitySignatureRule[];
  states: WorkflowState[];
  actions: WorkflowAction[];
  tags: string[];
  minimumConfidenceThreshold: number;
  createdBy: string;
  createdDate: string;
  lastModifiedDate: string;
  metadata: Record<string, unknown>;
  helpGuideIds: string[];
}

export interface ActivitySignatureRule {
  ruleId: string;
  eventType: number;
  recordKind: number;
  workspaceKind: string | null;
  required: boolean;
  weight: number;
  missingPenalty: number;
  maxAgeMinutes: number | null;
  description: string;
}

export interface WorkflowState {
  stateId: string;
  name: string;
  description: string;
  requiredEvidence: string[];
  sequence: number;
  isTerminal: boolean;
  nextStateId: string | null;
  helpGuideId: string;
  metadata: Record<string, unknown>;
}

export interface WorkflowAction {
  actionId: string;
  name: string;
  description: string;
  executionMode: number;
  availableInStateId: string;
  metadata: Record<string, unknown>;
}

export interface SerializableActiveEntity {
  recordId: number;
  name: string;
  recordKind: string;
  typeId: number;
  state: string | null;
  lastModified: string;
}

export interface SemanticWorkflowMatch {
  workflowId: string;
  workflowName: string;
  score: number;
  matchedTerms: string[];
  matchedFields: string[];
  reason: string;
}

export interface AmbiguitySignal {
  isAmbiguous: boolean;
  activityMatchId: string | null;
  semanticMatchId: string | null;
  activityConfidence: number;
  semanticScore: number;
  recommendedAction: string;
  clarificationQuestion: string | null;
}

export interface HelpGuideSection {
  sectionId: string;
  heading: string;
  level: number;
  content: string;
  relevantStepIds: string[];
}

export interface HealthCheckResponse {
  status: string;
  workflowCount: number;
}
