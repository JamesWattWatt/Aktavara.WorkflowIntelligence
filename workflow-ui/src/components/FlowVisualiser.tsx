import type { WorkflowCandidateResult } from '../types/api';

interface FlowVisualiserProps {
  candidate: WorkflowCandidateResult | null;
}

interface StepNode {
  type: 'matched' | 'missing';
  rule: string;
  evidence?: string;
  dotColor: 'grey' | 'blue' | 'green' | 'amber';
}

const getDotColor = (rule: string): 'grey' | 'blue' | 'green' | 'amber' => {
  const lower = rule.toLowerCase();
  if (lower.includes('search')) return 'grey';
  if (lower.includes('open') || lower.includes('workspace')) return 'blue';
  if (lower.includes('save') || lower.includes('create')) return 'green';
  if (lower.includes('modify') || lower.includes('edit')) return 'amber';
  return 'grey';
};

const getDotColorClass = (color: 'grey' | 'blue' | 'green' | 'amber'): string => {
  const map = {
    grey: 'bg-gray-500',
    blue: 'bg-blue-500',
    green: 'bg-green-500',
    amber: 'bg-amber-500'
  };
  return map[color];
};

const findMatchingEvidence = (rule: string, evidence: string[]): string | undefined => {
  const ruleLower = rule.toLowerCase();

  if (ruleLower.includes('search')) {
    return evidence.find(e => e.includes('SearchRecords'));
  }
  if (ruleLower.includes('open') || ruleLower.includes('workspace')) {
    return evidence.find(e => e.includes('OpenWorkspace'));
  }
  if (ruleLower.includes('save') || ruleLower.includes('create')) {
    return evidence.find(e => e.includes('SaveRecords'));
  }

  return evidence[0];
};

export const FlowVisualiser = ({ candidate }: FlowVisualiserProps) => {
  if (!candidate) {
    return null;
  }

  // Build step nodes
  const steps: StepNode[] = [
    ...candidate.matchedRules.map(rule => ({
      type: 'matched' as const,
      rule,
      evidence: findMatchingEvidence(rule, candidate.matchedEvidence),
      dotColor: getDotColor(rule)
    })),
    ...candidate.missingRules.map(rule => ({
      type: 'missing' as const,
      rule,
      dotColor: getDotColor(rule)
    }))
  ];

  return (
    <div className="w-full overflow-x-auto">
      <div className="flex gap-2 p-3" style={{ minWidth: 'min-content' }}>
        {steps.map((step, idx) => (
          <div key={idx} className="flex items-center gap-2">
            {/* Step Card */}
            <div
              className={`flex-shrink-0 flex flex-col gap-2 p-2 rounded border min-w-[140px] ${
                step.type === 'matched'
                  ? 'border-blue-400 dark:border-blue-500 bg-blue-50 dark:bg-blue-900/20'
                  : 'border-dashed border-gray-400 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50 opacity-60'
              }`}
            >
              <div className="flex items-center gap-2">
                <div className={`w-2.5 h-2.5 rounded-full ${getDotColorClass(step.dotColor)}`} />
                <p className="text-xs font-medium text-gray-900 dark:text-gray-100 truncate">{step.rule}</p>
              </div>
              <span className={`text-xs font-semibold px-1.5 py-0.5 rounded w-fit ${
                step.type === 'matched'
                  ? 'bg-blue-200 dark:bg-blue-900 text-blue-700 dark:text-blue-300'
                  : 'bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-400'
              }`}>
                {step.type === 'matched' ? '✓ Matched' : '✗ Missing'}
              </span>
              {step.evidence && (
                <p className="text-xs text-gray-600 dark:text-gray-400 truncate">{step.evidence}</p>
              )}
            </div>

            {/* Arrow */}
            {idx < steps.length - 1 && (
              <span className="text-gray-400 dark:text-gray-600 flex-shrink-0">→</span>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};
