import type { WorkflowCandidateResult } from '../types/api';

interface WorkflowListProps {
  candidates: WorkflowCandidateResult[];
  onSelect?: (candidate: WorkflowCandidateResult) => void;
  selectedId?: string;
}

export const WorkflowList = ({ candidates, onSelect, selectedId }: WorkflowListProps) => {
  if (candidates.length === 0) {
    return (
      <div className="p-4 text-center text-gray-500 dark:text-gray-400">
        <p>No candidates yet</p>
        <p className="text-sm mt-1">Upload a log file to see workflow candidates</p>
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
            p-3 rounded-lg border cursor-pointer transition-colors
            ${selectedId === candidate.workflowId
              ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
              : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
            }
          `}
        >
          <div className="flex justify-between items-start gap-2">
            <div className="text-left flex-1 min-w-0">
              <h3 className="font-medium text-sm truncate">{candidate.workflowName}</h3>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                State: {candidate.currentStateName || 'Unknown'}
              </p>
            </div>
            <div className="text-right flex-shrink-0">
              <div className="text-lg font-semibold">{(candidate.confidenceScore * 100).toFixed(0)}%</div>
              <p className="text-xs text-gray-500 dark:text-gray-400">{candidate.confidenceLevel}</p>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};
