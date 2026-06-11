import {
  AnalyzeResponse,
  WorkflowSummary,
  WorkflowDefinition,
  HelpGuide,
  HelpGuideSection,
  HealthResponse
} from './types/apiTypes.js';

const API_URL = process.env.AKTAVARA_API_URL || 'https://localhost:7200';
const API_KEY = process.env.AKTAVARA_API_KEY;

// Configure fetch for self-signed certificates in development
if (process.env.NODE_TLS_REJECT_UNAUTHORIZED === '0') {
  console.error('[API] WARNING: TLS certificate validation disabled for development');
}

async function makeRequest<T>(
  path: string,
  method: string = 'GET',
  body?: unknown
): Promise<T> {
  const url = `${API_URL}${path}`;

  console.error(`[API] ${method} ${path}`);

  const options: RequestInit = {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(API_KEY && { 'Authorization': `Bearer ${API_KEY}` })
    }
  };

  if (body) {
    options.body = JSON.stringify(body);
  }

  try {
    const response = await fetch(url, options);

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(
        `API error ${response.status}: ${response.statusText}\n${errorText}`
      );
    }

    return await response.json() as T;
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    throw new Error(`Failed to call ${path}: ${message}`);
  }
}

export async function analyzeText(
  logContent: string,
  userName: string,
  timeWindowMinutes: number = 30,
  userQuestion?: string
): Promise<AnalyzeResponse> {
  return makeRequest<AnalyzeResponse>(
    '/api/analyze/text',
    'POST',
    {
      logContent,
      userName,
      timeWindowMinutes,
      userQuestion
    }
  );
}

export async function getWorkflows(): Promise<WorkflowSummary[]> {
  return makeRequest<WorkflowSummary[]>('/api/workflows');
}

export async function getWorkflow(id: string): Promise<WorkflowDefinition> {
  return makeRequest<WorkflowDefinition>(`/api/workflows/${id}`);
}

export async function getHelpGuide(id: string): Promise<HelpGuide> {
  return makeRequest<HelpGuide>(`/api/help-guides/${id}`);
}

export async function getHelpGuideSection(
  workflowId: string,
  stepId: string
): Promise<HelpGuideSection[]> {
  const params = new URLSearchParams({ workflowId, stepId });
  return makeRequest<HelpGuideSection[]>(
    `/api/help-guides/section?${params.toString()}`
  );
}

export async function getHealth(): Promise<HealthResponse> {
  return makeRequest<HealthResponse>('/api/health');
}
