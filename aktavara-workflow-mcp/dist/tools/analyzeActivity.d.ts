import { z } from 'zod';
export declare const analyzeActivitySchema: z.ZodObject<{
    logContent: z.ZodString;
    userName: z.ZodString;
    timeWindowMinutes: z.ZodDefault<z.ZodOptional<z.ZodNumber>>;
    userQuestion: z.ZodOptional<z.ZodString>;
}, "strip", z.ZodTypeAny, {
    logContent: string;
    userName: string;
    timeWindowMinutes: number;
    userQuestion?: string | undefined;
}, {
    logContent: string;
    userName: string;
    timeWindowMinutes?: number | undefined;
    userQuestion?: string | undefined;
}>;
export type AnalyzeActivityInput = z.infer<typeof analyzeActivitySchema>;
export declare function analyzeActivity(input: AnalyzeActivityInput): Promise<string>;
