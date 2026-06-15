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

  // Match by rule keywords
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
    return (
      <div className="p-6 text-center text-gray-500 dark:text-gray-400 flex items-center justify-center min-h-[400px]">
        <p>Select a workflow candidate to see the flow</p>
      </div>
    );
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

  const total = candidate.matchedRules.length + candidate.missingRules.length;
  const matched = candidate.matchedRules.length;

  // Build confidence explanation
  let explanation = `${matched} of ${total} rules matched. ${candidate.confidenceLevel} confidence (${(candidate.confidenceScore * 100).toFixed(0)}%).`;

  if ((candidate.scoreBreakdown['Sequence Bonus'] || 0) > 0) {
    explanation += ' Steps detected in correct sequence (+bonus).';
  }
  if ((candidate.scoreBreakdown['Entity Correlation'] || 0) > 0) {
    explanation += ' Related records confirmed in same session (+bonus).';
  }
  if (candidate.missingRules.length > 0) {
    explanation += ` Missing: ${candidate.missingRules[0]}`;
  }

  return (
    <div className="p-6 space-y-6 flex flex-col">
      {/* Step Flow */}
      <div>
        <h3 className="font-semibold mb-4 text-sm">Detected Steps</h3>
        <div className="space-y-4">
          {steps.map((step, idx) => (
            <div key={idx}>
              {/* Step Node */}
              <div
                className={`
                  rounded-lg border-l-4 p-4 transition-all
                  ${step.type === 'matched'
                    ? 'border-green-500 bg-gray-800 dark:bg-gray-750'
                    : 'border-amber-500 bg-gray-900 dark:bg-gray-800 opacity-60'
                  }
                `}
              >
                <div className="flex items-start gap-3">
                  {/* Dot */}
                  <div className={`flex-shrink-0 w-3 h-3 rounded-full mt-1 ${getDotColorClass(step.dotColor)}`} />

                  {/* Content */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2">
                      <p className="text-sm text-gray-200 dark:text-gray-300">{step.rule}</p>
                      <span className={`flex-shrink-0 text-xs font-semibold px-2 py-1 rounded-full whitespace-nowrap ${
                        step.type === 'matched'
                          ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400'
                          : 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400'
                      }`}>
                        {step.type === 'matched' ? '✓ Matched' : '✗ Missing'}
                      </span>
                    </div>

                    {/* Evidence */}
                    {step.evidence && step.type === 'matched' && (
                      <p className="text-xs text-gray-400 dark:text-gray-500 mt-2">
                        {step.evidence}
                      </p>
                    )}
                  </div>
                </div>
              </div>

              {/* Connector line and arrow */}
              {idx < steps.length - 1 && (
                <div className="flex justify-center py-1">
                  <div className="w-px h-4 bg-gray-600 dark:bg-gray-500"></div>
                </div>
              )}
              {idx < steps.length - 1 && (
                <div className="text-center text-gray-500 dark:text-gray-400 -my-3">▼</div>
              )}
            </div>
          ))}
        </div>
      </div>

      {/* Current State Indicator */}
      {candidate.currentStateName && (
        <div className="p-3 bg-blue-100 dark:bg-blue-900/30 border border-blue-300 dark:border-blue-800 rounded-lg text-xs text-blue-700 dark:text-blue-400">
          <p className="font-semibold">📍 Current State</p>
          <p className="mt-1">{candidate.currentStateName}</p>
        </div>
      )}

      {/* Evidence Tags */}
      {candidate.matchedEvidence.length > 0 && (
        <div>
          <p className="text-xs font-semibold text-gray-400 dark:text-gray-500 uppercase mb-2">Matched Evidence</p>
          <div className="flex flex-wrap gap-2">
            {candidate.matchedEvidence.slice(0, 5).map((evidence, idx) => (
              <span
                key={idx}
                className="text-xs px-2 py-1 rounded-full bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400"
              >
                ✓ {evidence}
              </span>
            ))}
            {candidate.matchedEvidence.length > 5 && (
              <span className="text-xs px-2 py-1 text-gray-500 dark:text-gray-400">
                +{candidate.matchedEvidence.length - 5} more
              </span>
            )}
          </div>
        </div>
      )}

      {/* Confidence Explanation */}
      <div className="p-3 bg-blue-100 dark:bg-blue-900/20 border border-blue-300 dark:border-blue-800 rounded-lg text-xs text-blue-700 dark:text-blue-400">
        {explanation}
      </div>

      {/* Next Step Hint */}
      {candidate.nextStepHint && (
        <div className="p-3 bg-amber-100 dark:bg-amber-900/30 border border-amber-300 dark:border-amber-800 rounded-lg text-sm text-amber-700 dark:text-amber-400 font-semibold">
          → {candidate.nextStepHint}
        </div>
      )}
    </div>
  );
};
