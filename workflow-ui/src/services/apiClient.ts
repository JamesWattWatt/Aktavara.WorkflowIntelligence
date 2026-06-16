import type {
  AnalyzeResponse,
  WorkflowSummary,
  WorkflowDefinition,
  HelpGuideSection,
  HealthCheckResponse,
  GuideSuggestion,
  WorkflowLibraryItem,
  WorkflowGuideMappings,
  InferredWorkflowSuggestion,
  InferredNameSuggestion
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

  async suggestGuideMapping(
    workflowId: string,
    workflowName: string,
    stepId: string,
    currentStateName: string,
    matchedRules: string[] = [],
    matchedEvidence: string[] = []
  ): Promise<GuideSuggestion> {
    const response = await fetch(`${BASE_URL}/help-guides/suggest`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        workflowId,
        workflowName,
        stepId,
        currentStateName,
        matchedRules,
        matchedEvidence
      })
    });

    if (!response.ok) {
      throw new Error(`Failed to suggest guide mapping: ${response.statusText}`);
    }

    return response.json();
  }

  async saveGuideMapping(
    workflowId: string,
    stepId: string,
    guideFile: string,
    sectionId?: string
  ): Promise<void> {
    const response = await fetch(`${BASE_URL}/help-guides/mapping`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        workflowId,
        stepId,
        guideFile,
        sectionId: sectionId || null
      })
    });

    if (!response.ok) {
      throw new Error(`Failed to save guide mapping: ${response.statusText}`);
    }
  }

  async getWorkflowLibrary(): Promise<WorkflowLibraryItem[]> {
    const response = await fetch(`${BASE_URL}/workflows/library`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch workflow library: ${response.statusText}`);
    }

    return response.json();
  }

  async updateWorkflow(
    id: string,
    definition: WorkflowDefinition
  ): Promise<WorkflowLibraryItem> {
    const response = await fetch(`${BASE_URL}/workflows/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(definition)
    });

    if (!response.ok) {
      throw new Error(`Failed to update workflow: ${response.statusText}`);
    }

    return response.json();
  }

  async createWorkflow(definition: WorkflowDefinition): Promise<WorkflowLibraryItem> {
    const response = await fetch(`${BASE_URL}/workflows`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(definition)
    });

    if (!response.ok) {
      throw new Error(`Failed to create workflow: ${response.statusText}`);
    }

    return response.json();
  }

  async deleteWorkflow(id: string, permanent: boolean = false): Promise<void> {
    const response = await fetch(`${BASE_URL}/workflows/${id}?permanent=${permanent}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`Failed to delete workflow: ${response.statusText}`);
    }
  }

  async inferWorkflow(logContent: string): Promise<InferredWorkflowSuggestion> {
    const response = await fetch(`${BASE_URL}/workflows/infer`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ rawLogContent: logContent })
    });

    if (!response.ok) {
      throw new Error(`Failed to infer workflow: ${response.statusText}`);
    }

    return response.json();
  }

  async inferWorkflowName(suggestion: InferredWorkflowSuggestion): Promise<InferredNameSuggestion> {
    const response = await fetch(`${BASE_URL}/workflows/infer/name`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        suggestedRules: suggestion.suggestedRules,
        suggestedTags: suggestion.suggestedTags,
        evidenceSessions: suggestion.evidenceSessions
      })
    });

    if (!response.ok) {
      throw new Error(`Failed to infer workflow name: ${response.statusText}`);
    }

    return response.json();
  }

  async generateWorkflowQuestions(id: string, stateId?: string): Promise<WorkflowDefinition> {
    const url = stateId
      ? `${BASE_URL}/workflows/${id}/generate-questions?stateId=${encodeURIComponent(stateId)}`
      : `${BASE_URL}/workflows/${id}/generate-questions`;

    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`Failed to generate questions: ${response.statusText}`);
    }

    return response.json();
  }

  async getWorkflowGuideMappings(workflowId: string): Promise<WorkflowGuideMappings> {
    const response = await fetch(`${BASE_URL}/workflows/${workflowId}/guide-mappings`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch guide mappings: ${response.statusText}`);
    }

    return response.json();
  }
}

export const apiClient = new ApiClient();
