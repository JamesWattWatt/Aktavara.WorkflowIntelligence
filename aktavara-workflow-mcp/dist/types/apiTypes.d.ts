export interface AnalyzeResponse {
    sessionId: string;
    fileName: string;
    parsedAt: string;
    totalEntries: number;
    totalEvents: number;
    durationMs: number;
    currentState: string;
    guidanceLevel: string;
    recommendedNextStep?: string;
    contextNarrative: string;
    activeEntities: SerializableActiveEntity[];
    workflowHints: string[];
    workflowCandidates: WorkflowCandidateResult[];
    semanticMatches: SemanticWorkflowMatch[];
    ambiguity?: AmbiguitySignal;
}
export interface SerializableActiveEntity {
    recordKind: string;
    typeId: string;
    recordId: string;
    name: string;
    state?: string;
    lastModified: string;
}
export interface WorkflowCandidateResult {
    workflowId: string;
    workflowName: string;
    confidenceScore: number;
    confidenceLevel: string;
    currentStateName?: string;
    matchedRules: string[];
    matchedEvidence: string[];
    missingRules: string[];
    nextStepHint?: string;
    scoreBreakdown: Record<string, number>;
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
    activityMatchId?: string;
    semanticMatchId?: string;
    activityConfidence: number;
    semanticScore: number;
    recommendedAction: string;
    clarificationQuestion?: string;
}
export interface WorkflowSummary {
    id: string;
    name: string;
    status: string;
    version: string;
    description: string;
    riskLevel: string;
    tags: string[];
    isValid: boolean;
    validationErrors: string[];
    ruleCount: number;
    stateCount: number;
    confidenceThreshold: number;
}
export interface WorkflowDefinition {
    workflowId: string;
    name: string;
    description?: string;
    tags: string[];
    activitySignature: WorkflowSignatureRule[];
    states: WorkflowStateDefinition[];
}
export interface WorkflowSignatureRule {
    eventType: string;
    description: string;
    weight: number;
    required: boolean;
    missingPenalty: number;
}
export interface WorkflowStateDefinition {
    stateId: string;
    name: string;
    sequence: number;
    isTerminal: boolean;
    nextStateId?: string;
    requiredEvidence: string[];
    helpGuideId?: string;
}
export interface HelpGuide {
    helpGuideId: string;
    title: string;
    fileName: string;
    workspaceType: string;
    markdownContent: string;
    sections: HelpGuideSection[];
    lastModified: string;
}
export interface HelpGuideSection {
    sectionId: string;
    heading: string;
    level: number;
    content: string;
    parentSectionId?: string;
    relevantStepIds: string[];
}
export interface HealthResponse {
    status: string;
    workflowCount: number;
    timestamp: string;
    version: string;
}
