#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  ListToolsRequest,
  CallToolRequest,
  ListResourcesRequest,
  ReadResourceRequest,
  Tool,
  TextContent
} from '@modelcontextprotocol/sdk/types.js';

import * as analyzeActivityTool from './tools/analyzeActivity.js';
import * as detectTaskTool from './tools/detectTask.js';
import * as getWorkflowTool from './tools/getWorkflow.js';
import * as getHelpGuideTool from './tools/getHelpGuide.js';
import * as explainNextStepTool from './tools/explainNextStep.js';

import * as workflowResources from './resources/workflowResources.js';
import * as helpGuideResources from './resources/helpGuideResources.js';

const API_URL = process.env.AKTAVARA_API_URL || 'https://localhost:7200';

const server = new Server(
  {
    name: 'aktavara-workflow-mcp',
    version: '1.0.0'
  },
  {
    capabilities: {
      tools: {},
      resources: {}
    }
  }
);

// Tool handlers
server.setRequestHandler('tools/list' as any, async () => {
  const tools: Tool[] = [
    {
      name: 'analyze_activity',
      description:
        'Analyze recent Aktavara user activity from a log snippet to detect workflow patterns and provide guidance',
      inputSchema: {
        type: 'object',
        properties: {
          logContent: {
            type: 'string',
            description: 'Raw activity log text to analyze'
          },
          userName: {
            type: 'string',
            description: 'The Aktavara user name'
          },
          timeWindowMinutes: {
            type: 'number',
            description: 'Time window in minutes to consider (default 30)',
            default: 30
          },
          userQuestion: {
            type: 'string',
            description:
              'Optional: what the user says they are trying to do'
          }
        },
        required: ['logContent', 'userName']
      }
    },
    {
      name: 'detect_current_task',
      description:
        'Detect what workflow task the user is currently performing based on their activity and optional description',
      inputSchema: {
        type: 'object',
        properties: {
          logContent: {
            type: 'string',
            description: 'Raw activity log text'
          },
          userName: {
            type: 'string',
            description: 'The Aktavara user name'
          },
          userQuestion: {
            type: 'string',
            description:
              'Optional: what the user says they are trying to do'
          },
          timeWindowMinutes: {
            type: 'number',
            description: 'Time window in minutes (default 30)',
            default: 30
          }
        },
        required: ['logContent', 'userName']
      }
    },
    {
      name: 'get_workflow_definition',
      description:
        'Get the full definition of a named workflow including its steps, states, and rules',
      inputSchema: {
        type: 'object',
        properties: {
          workflowId: {
            type: 'string',
            description: 'The workflow ID (e.g., "update-node-in-path")'
          }
        },
        required: ['workflowId']
      }
    },
    {
      name: 'get_help_guide',
      description:
        'Get help guide content for a specific Aktavara workspace or feature',
      inputSchema: {
        type: 'object',
        properties: {
          helpGuideId: {
            type: 'string',
            description: 'The help guide ID (e.g., "Path_Workspace")'
          },
          sectionId: {
            type: 'string',
            description:
              'Optional: specific section ID within the guide'
          }
        },
        required: ['helpGuideId']
      }
    },
    {
      name: 'explain_next_step',
      description:
        'Get a complete explanation of what the user should do next, grounded in their current activity and the relevant help documentation',
      inputSchema: {
        type: 'object',
        properties: {
          logContent: {
            type: 'string',
            description: 'Raw activity log text'
          },
          userName: {
            type: 'string',
            description: 'The Aktavara user name'
          },
          userQuestion: {
            type: 'string',
            description:
              'Optional: what the user says they are trying to do'
          },
          timeWindowMinutes: {
            type: 'number',
            description: 'Time window in minutes (default 30)',
            default: 30
          }
        },
        required: ['logContent', 'userName']
      }
    }
  ];

  return { tools };
});

server.setRequestHandler('tools/call' as any, async (request: any) => {
  try {
    let result: string;

    switch (request.params.name) {
      case 'analyze_activity':
        result = await analyzeActivityTool.analyzeActivity(
          analyzeActivityTool.analyzeActivitySchema.parse(
            request.params.arguments
          )
        );
        break;

      case 'detect_current_task':
        result = await detectTaskTool.detectTask(
          detectTaskTool.detectTaskSchema.parse(request.params.arguments)
        );
        break;

      case 'get_workflow_definition':
        result = await getWorkflowTool.getWorkflow(
          getWorkflowTool.getWorkflowSchema.parse(request.params.arguments)
        );
        break;

      case 'get_help_guide':
        result = await getHelpGuideTool.getHelpGuide(
          getHelpGuideTool.getHelpGuideSchema.parse(request.params.arguments)
        );
        break;

      case 'explain_next_step':
        result = await explainNextStepTool.explainNextStep(
          explainNextStepTool.explainNextStepSchema.parse(
            request.params.arguments
          )
        );
        break;

      default:
        return {
          content: [
            {
              type: 'text',
              text: `Unknown tool: ${request.params.name}`
            }
          ],
          isError: true
        };
    }

    return {
      content: [
        {
          type: 'text',
          text: result
        }
      ]
    };
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    return {
      content: [
        {
          type: 'text',
          text: `Error: ${message}`
        }
      ],
      isError: true
    };
  }
});

// Resource handlers
server.setRequestHandler('resources/list' as any, async () => {
  const workflowResources_list = await workflowResources.createWorkflowResources();
  const helpGuideResources_list = await helpGuideResources.createHelpGuideResources();

  return {
    resources: [...workflowResources_list, ...helpGuideResources_list]
  };
});

server.setRequestHandler('resources/read' as any, async (request: any) => {
  const workflowContent = await workflowResources.getWorkflowResourceContent(
    request.params.uri
  );

  if (workflowContent) {
    return workflowContent;
  }

  const helpGuideContent = await helpGuideResources.getHelpGuideResourceContent(
    request.params.uri
  );

  if (helpGuideContent) {
    return helpGuideContent;
  }

  throw new Error(`Unknown resource: ${request.params.uri}`);
});

// Start server
async function main() {
  console.error(`[MCP] Starting Aktavara Workflow MCP Server v1.0.0`);
  console.error(`[MCP] API URL: ${API_URL}`);
  console.error('[MCP] Connecting via stdio transport...');

  const transport = new StdioServerTransport();
  await server.connect(transport);

  console.error('[MCP] Server running. Ready to handle requests.');
}

main().catch((error) => {
  console.error('[MCP] Fatal error:', error);
  process.exit(1);
});
