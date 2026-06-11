const API_URL = process.env.AKTAVARA_API_URL || 'https://localhost:7200';
const API_KEY = process.env.AKTAVARA_API_KEY;
// Configure fetch for self-signed certificates in development
if (process.env.NODE_TLS_REJECT_UNAUTHORIZED === '0') {
    console.error('[API] WARNING: TLS certificate validation disabled for development');
}
async function makeRequest(path, method = 'GET', body) {
    const url = `${API_URL}${path}`;
    console.error(`[API] ${method} ${path}`);
    const options = {
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
            throw new Error(`API error ${response.status}: ${response.statusText}\n${errorText}`);
        }
        return await response.json();
    }
    catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        throw new Error(`Failed to call ${path}: ${message}`);
    }
}
export async function analyzeText(logContent, userName, timeWindowMinutes = 30, userQuestion) {
    return makeRequest('/api/analyze/text', 'POST', {
        logContent,
        userName,
        timeWindowMinutes,
        userQuestion
    });
}
export async function getWorkflows() {
    return makeRequest('/api/workflows');
}
export async function getWorkflow(id) {
    return makeRequest(`/api/workflows/${id}`);
}
export async function getHelpGuide(id) {
    return makeRequest(`/api/help-guides/${id}`);
}
export async function getHelpGuideSection(workflowId, stepId) {
    const params = new URLSearchParams({ workflowId, stepId });
    return makeRequest(`/api/help-guides/section?${params.toString()}`);
}
export async function getHealth() {
    return makeRequest('/api/health');
}
//# sourceMappingURL=apiClient.js.map