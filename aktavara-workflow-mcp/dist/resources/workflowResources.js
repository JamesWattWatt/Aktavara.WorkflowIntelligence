import * as api from '../apiClient.js';
export async function createWorkflowResources() {
    const resources = [];
    // akta://workflows - list all workflows
    resources.push({
        uri: 'akta://workflows',
        name: 'All Workflows',
        description: 'List of all available workflow definitions',
        mimeType: 'application/json'
    });
    // akta://health - health check
    resources.push({
        uri: 'akta://health',
        name: 'API Health',
        description: 'Health status of the Aktavara API',
        mimeType: 'application/json'
    });
    try {
        const workflows = await api.getWorkflows();
        // akta://workflows/{workflowId} - individual workflow
        workflows.forEach(wf => {
            resources.push({
                uri: `akta://workflows/${wf.id}`,
                name: wf.name,
                description: wf.description || `Workflow definition for ${wf.name}`,
                mimeType: 'application/json'
            });
        });
    }
    catch (error) {
        console.error('[Resources] Could not fetch workflows:', error);
    }
    return resources;
}
export async function getWorkflowResourceContent(uri) {
    if (uri === 'akta://workflows') {
        const workflows = await api.getWorkflows();
        return {
            uri,
            mimeType: 'application/json',
            text: JSON.stringify(workflows, null, 2)
        };
    }
    if (uri === 'akta://health') {
        const health = await api.getHealth();
        return {
            uri,
            mimeType: 'application/json',
            text: JSON.stringify(health, null, 2)
        };
    }
    const workflowMatch = uri.match(/^akta:\/\/workflows\/(.+)$/);
    if (workflowMatch) {
        const workflowId = workflowMatch[1];
        const workflow = await api.getWorkflow(workflowId);
        return {
            uri,
            mimeType: 'application/json',
            text: JSON.stringify(workflow, null, 2)
        };
    }
    return null;
}
//# sourceMappingURL=workflowResources.js.map