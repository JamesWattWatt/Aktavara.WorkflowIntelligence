import { z } from 'zod';
export declare const getWorkflowSchema: z.ZodObject<{
    workflowId: z.ZodString;
}, "strip", z.ZodTypeAny, {
    workflowId: string;
}, {
    workflowId: string;
}>;
export type GetWorkflowInput = z.infer<typeof getWorkflowSchema>;
export declare function getWorkflow(input: GetWorkflowInput): Promise<string>;
