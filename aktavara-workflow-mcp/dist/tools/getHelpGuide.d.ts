import { z } from 'zod';
export declare const getHelpGuideSchema: z.ZodObject<{
    helpGuideId: z.ZodString;
    sectionId: z.ZodOptional<z.ZodString>;
}, "strip", z.ZodTypeAny, {
    helpGuideId: string;
    sectionId?: string | undefined;
}, {
    helpGuideId: string;
    sectionId?: string | undefined;
}>;
export type GetHelpGuideInput = z.infer<typeof getHelpGuideSchema>;
export declare function getHelpGuide(input: GetHelpGuideInput): Promise<string>;
