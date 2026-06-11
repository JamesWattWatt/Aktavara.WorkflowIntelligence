import { z } from 'zod';
import * as api from '../apiClient.js';
export const getHelpGuideSchema = z.object({
    helpGuideId: z.string()
        .describe('The help guide ID (e.g., "Path_Workspace")'),
    sectionId: z.string().optional()
        .describe('Optional: specific section ID within the guide')
});
export async function getHelpGuide(input) {
    const guide = await api.getHelpGuide(input.helpGuideId);
    const lines = [];
    lines.push(`# ${guide.title}`);
    lines.push('');
    lines.push(`**Type:** ${guide.workspaceType} Workspace`);
    lines.push(`**Last Updated:** ${new Date(guide.lastModified).toLocaleDateString()}`);
    lines.push('');
    // If a specific section is requested, return just that section
    if (input.sectionId && guide.sections.length > 0) {
        const section = guide.sections.find(s => s.sectionId === input.sectionId);
        if (!section) {
            return `Error: Section "${input.sectionId}" not found in ${input.helpGuideId}`;
        }
        lines.push(`## ${section.heading}`);
        lines.push('');
        lines.push(section.content);
        return lines.join('\n');
    }
    // If no section requested, return table of contents
    if (guide.sections.length > 0) {
        lines.push('## Contents');
        lines.push('');
        // Group sections by parent
        const topLevelSections = guide.sections.filter(s => !s.parentSectionId);
        topLevelSections.forEach(section => {
            lines.push(`- [${section.heading}](#${section.sectionId})`);
            // Add child sections
            const childSections = guide.sections.filter(s => s.parentSectionId === section.sectionId);
            childSections.forEach(child => {
                lines.push(`  - [${child.heading}](#${child.sectionId})`);
            });
        });
        lines.push('');
        // Add full content
        guide.sections.forEach(section => {
            const heading = '#'.repeat(section.level + 1);
            lines.push(`${heading} ${section.heading}`);
            lines.push('');
            lines.push(section.content);
            lines.push('');
        });
    }
    else {
        lines.push(guide.markdownContent);
    }
    return lines.join('\n');
}
//# sourceMappingURL=getHelpGuide.js.map