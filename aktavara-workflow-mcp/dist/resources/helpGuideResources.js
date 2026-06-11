import * as api from '../apiClient.js';
// Cache help guides to avoid repeated fetches
const helpGuideCache = new Map();
export async function createHelpGuideResources() {
    const resources = [];
    // Common help guides (these are known to exist based on the architecture)
    const knownGuides = [
        'Path_Workspace',
        'Topology_Workspace',
        'General'
    ];
    knownGuides.forEach(guideId => {
        resources.push({
            uri: `akta://help-guides/${guideId}`,
            name: guideId.replace(/_/g, ' '),
            description: `Help guide for ${guideId}`,
            mimeType: 'text/markdown'
        });
    });
    return resources;
}
export async function getHelpGuideResourceContent(uri) {
    const helpGuideMatch = uri.match(/^akta:\/\/help-guides\/(.+)$/);
    if (!helpGuideMatch) {
        return null;
    }
    const helpGuideId = helpGuideMatch[1];
    try {
        const guide = await api.getHelpGuide(helpGuideId);
        // Format as markdown with sections
        const lines = [];
        lines.push(`# ${guide.title}`);
        lines.push('');
        lines.push(`**Type:** ${guide.workspaceType}`);
        lines.push('');
        if (guide.sections && guide.sections.length > 0) {
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
        return {
            uri,
            mimeType: 'text/markdown',
            text: lines.join('\n')
        };
    }
    catch (error) {
        console.error(`[Resources] Could not fetch help guide ${helpGuideId}:`, error);
        return null;
    }
}
//# sourceMappingURL=helpGuideResources.js.map