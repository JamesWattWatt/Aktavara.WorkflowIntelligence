import type {
  AnalyzeResponse,
  WorkflowSummary,
  WorkflowDefinition,
  HelpGuideSection,
  HealthCheckResponse
} from '../types/api';

const BASE_URL = '/api';

class ApiClient {
  async uploadLogFile(file: File): Promise<AnalyzeResponse> {
    const formData = new FormData();
    formData.append('logFile', file);

    const response = await fetch(`${BASE_URL}/analyze/upload`, {
      method: 'POST',
      body: formData
    });

    if (!response.ok) {
      throw new Error(`Failed to upload log file: ${response.statusText}`);
    }

    return response.json();
  }

  async getWorkflows(): Promise<WorkflowSummary[]> {
    const response = await fetch(`${BASE_URL}/workflows`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch workflows: ${response.statusText}`);
    }

    return response.json();
  }

  async getWorkflow(id: string): Promise<WorkflowDefinition> {
    const response = await fetch(`${BASE_URL}/workflows/${id}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch workflow: ${response.statusText}`);
    }

    return response.json();
  }

  async updateWorkflowStatus(
    id: string,
    status: 'Approved' | 'Candidate' | 'Deprecated'
  ): Promise<WorkflowSummary> {
    const response = await fetch(`${BASE_URL}/workflows/${id}/status`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ status })
    });

    if (!response.ok) {
      throw new Error(`Failed to update workflow status: ${response.statusText}`);
    }

    return response.json();
  }

  async getHelpGuideSection(
    workflowId: string,
    stepId: string
  ): Promise<HelpGuideSection[]> {
    const params = new URLSearchParams({
      workflowId,
      stepId
    });

    const response = await fetch(`${BASE_URL}/help-guides/section?${params}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch help guide sections: ${response.statusText}`);
    }

    return response.json();
  }

  async getHealth(): Promise<HealthCheckResponse> {
    const response = await fetch(`${BASE_URL}/health`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`Health check failed: ${response.statusText}`);
    }

    return response.json();
  }
}

export const apiClient = new ApiClient();
