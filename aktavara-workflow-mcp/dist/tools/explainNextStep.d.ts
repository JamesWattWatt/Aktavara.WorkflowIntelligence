import { z } from 'zod';
export declare const explainNextStepSchema: z.ZodObject<{
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
export type ExplainNextStepInput = z.infer<typeof explainNextStepSchema>;
export declare function explainNextStep(input: ExplainNextStepInput): Promise<string>;
