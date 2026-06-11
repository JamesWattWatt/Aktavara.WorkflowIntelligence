import { z } from 'zod';
export declare const detectTaskSchema: z.ZodObject<{
    logContent: z.ZodString;
    userName: z.ZodString;
    userQuestion: z.ZodOptional<z.ZodString>;
    timeWindowMinutes: z.ZodDefault<z.ZodOptional<z.ZodNumber>>;
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
export type DetectTaskInput = z.infer<typeof detectTaskSchema>;
export declare function detectTask(input: DetectTaskInput): Promise<string>;
