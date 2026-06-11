#!/usr/bin/env node
import * as api from './apiClient.js';
const testLogContent = `[2026-06-10T10:00:00] User: testuser | Session: sess-001 | Action: OpenWorkspace | Workspace: Path | Status: Success
[2026-06-10T10:01:00] User: testuser | Session: sess-001 | Action: SearchRecords | Filter: type=Node | Count: 5 | Status: Success
[2026-06-10T10:02:00] User: testuser | Session: sess-001 | Action: SelectRecord | Record: node-123 | Status: Success
[2026-06-10T10:03:00] User: testuser | Session: sess-001 | Action: ModifyRecord | Record: node-123 | Changes: name | Status: Success
[2026-06-10T10:04:00] User: testuser | Session: sess-001 | Action: SaveRecords | Count: 1 | Status: Success`;
async function runTests() {
    console.log('🧪 Starting manual MCP tests...\n');
    try {
        // Test 1: Health check
        console.log('1️⃣  Testing API health...');
        const health = await api.getHealth();
        console.log(`   ✓ Status: ${health.status}`);
        console.log(`   ✓ Workflows loaded: ${health.workflowCount}\n`);
        // Test 2: List workflows
        console.log('2️⃣  Fetching workflow list...');
        const workflows = await api.getWorkflows();
        console.log(`   ✓ Found ${workflows.length} workflows`);
        if (workflows.length > 0) {
            console.log(`   ✓ First workflow: ${workflows[0].name}\n`);
        }
        // Test 3: Get workflow definition
        console.log('3️⃣  Fetching workflow definition...');
        const workflow = await api.getWorkflow('update-node-in-path');
        console.log(`   ✓ Workflow: ${workflow.name}`);
        console.log(`   ✓ States: ${workflow.states.length}\n`);
        // Test 4: Analyze activity without question
        console.log('4️⃣  Analyzing activity (without question)...');
        const analysisNoQ = await api.analyzeText(testLogContent, 'testuser', 30);
        console.log(`   ✓ Current state: ${analysisNoQ.currentState}`);
        console.log(`   ✓ Top match: ${analysisNoQ.workflowCandidates[0]?.workflowName || 'None'}`);
        console.log(`   ✓ Guidance level: ${analysisNoQ.guidanceLevel}\n`);
        // Test 5: Analyze activity with question
        console.log('5️⃣  Analyzing activity (with question)...');
        const analysisWithQ = await api.analyzeText(testLogContent, 'testuser', 30, 'I want to update a node property');
        console.log(`   ✓ Semantic matches: ${analysisWithQ.semanticMatches.length}`);
        if (analysisWithQ.ambiguity) {
            console.log(`   ✓ Ambiguity detected: ${analysisWithQ.ambiguity.isAmbiguous}`);
        }
        console.log('');
        // Test 6: Get help guide
        console.log('6️⃣  Fetching help guide...');
        const guide = await api.getHelpGuide('Path_Workspace');
        console.log(`   ✓ Guide: ${guide.title}`);
        console.log(`   ✓ Sections: ${guide.sections.length}\n`);
        // Test 7: Get help guide section
        console.log('7️⃣  Fetching help guide section...');
        const sections = await api.getHelpGuideSection('update-node-in-path', 'editing');
        console.log(`   ✓ Found ${sections.length} relevant sections\n`);
        console.log('✅ All tests passed!');
    }
    catch (error) {
        console.error('❌ Test failed:', error);
        process.exit(1);
    }
}
runTests();
//# sourceMappingURL=test-manual.js.map