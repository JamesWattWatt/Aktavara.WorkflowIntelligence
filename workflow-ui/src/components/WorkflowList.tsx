import type { WorkflowCandidateResult } from '../types/api';

interface WorkflowListProps {
  candidates: WorkflowCandidateResult[];
  onSelect?: (candidate: WorkflowCandidateResult) => void;
  selectedId?: string;
}

const getConfidenceColor = (score: number): string => {
  if (score >= 0.85) return 'bg-[#43A047]';
  if (score >= 0.55) return 'bg-[#FB8C00]';
  return 'bg-[#E22A11]';
};

export const WorkflowList = ({ candidates, onSelect, selectedId }: WorkflowListProps) => {
  if (candidates.length === 0) {
    return (
      <div className="p-4 text-center text-gray-500 dark:text-gray-400 text-sm">
        <p className="font-medium">No candidates yet</p>
        <p className="mt-1">Drop a log file to discover workflow patterns</p>
      </div>
    );
  }

  return (
    <div className="space-y-2 p-4">
      {candidates.map((candidate) => (
        <div
          key={candidate.workflowId}
          onClick={() => onSelect?.(candidate)}
          className={`
            p-3 rounded-lg border cursor-pointer transition-all
            ${selectedId === candidate.workflowId
              ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20 ring-1 ring-blue-500'
              : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
            }
          `}
        >
          {/* Selected indicator */}
          {selectedId === candidate.workflowId && (
            <div className="text-xs font-semibold text-blue-600 dark:text-blue-400 mb-2">
              ✓ Selected
            </div>
          )}

          <div className="space-y-2">
            {/* Title and state */}
            <div className="flex justify-between items-start gap-2">
              <div className="text-left flex-1 min-w-0">
                <h3 className="font-medium text-sm truncate">{candidate.workflowName}</h3>
                {candidate.currentStateName && (
                  <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                    {candidate.currentStateName}
                  </p>
                )}
              </div>
              <div className="text-right flex-shrink-0">
                <div className="text-lg font-semibold">{(candidate.confidenceScore * 100).toFixed(0)}%</div>
                <p className="text-xs text-gray-500 dark:text-gray-400">{candidate.confidenceLevel}</p>
              </div>
            </div>

            {/* Confidence progress bar */}
            <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
              <div
                className={`h-full ${getConfidenceColor(candidate.confidenceScore)} transition-all`}
                style={{ width: `${candidate.confidenceScore * 100}%` }}
              />
            </div>

            {/* Evidence count */}
            <div className="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-400">
              <span>
                {candidate.matchedEvidence.length > 0
                  ? `${candidate.matchedEvidence.length} matched • `
                  : ''}
                {candidate.matchedRules.length} rules
              </span>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};
