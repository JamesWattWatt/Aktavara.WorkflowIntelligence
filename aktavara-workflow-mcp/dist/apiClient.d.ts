import { AnalyzeResponse, WorkflowSummary, WorkflowDefinition, HelpGuide, HelpGuideSection, HealthResponse } from './types/apiTypes.js';
export declare function analyzeText(logContent: string, userName: string, timeWindowMinutes?: number, userQuestion?: string): Promise<AnalyzeResponse>;
export declare function getWorkflows(): Promise<WorkflowSummary[]>;
export declare function getWorkflow(id: string): Promise<WorkflowDefinition>;
export declare function getHelpGuide(id: string): Promise<HelpGuide>;
export declare function getHelpGuideSection(workflowId: string, stepId: string): Promise<HelpGuideSection[]>;
export declare function getHealth(): Promise<HealthResponse>;
