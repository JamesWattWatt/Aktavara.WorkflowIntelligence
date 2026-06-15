import type { WorkflowCandidateResult } from '../types/api';

interface WorkflowDetailProps {
  candidate: WorkflowCandidateResult | null;
}

export const WorkflowDetail = ({ candidate }: WorkflowDetailProps) => {
  if (!candidate) {
    return (
      <div className="p-6 text-center text-gray-500 dark:text-gray-400">
        <p>Select a workflow to see details</p>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-xl font-bold mb-2">{candidate.workflowName}</h2>
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <p className="text-gray-500 dark:text-gray-400">Confidence</p>
            <p className="font-semibold">{(candidate.confidenceScore * 100).toFixed(0)}%</p>
          </div>
          <div>
            <p className="text-gray-500 dark:text-gray-400">Level</p>
            <p className="font-semibold">{candidate.confidenceLevel}</p>
          </div>
        </div>
      </div>

      {candidate.currentStateName && (
        <div>
          <h3 className="font-semibold mb-2">Current State</h3>
          <p className="text-sm">{candidate.currentStateName}</p>
        </div>
      )}

      {candidate.matchedRules.length > 0 && (
        <div>
          <h3 className="font-semibold mb-2">Matched Rules ({candidate.matchedRules.length})</h3>
          <ul className="space-y-1 text-sm">
            {candidate.matchedRules.map((rule, idx) => (
              <li key={idx} className="text-green-600 dark:text-green-400">✓ {rule}</li>
            ))}
          </ul>
        </div>
      )}

      {candidate.missingRules.length > 0 && (
        <div>
          <h3 className="font-semibold mb-2">Missing Rules ({candidate.missingRules.length})</h3>
          <ul className="space-y-1 text-sm">
            {candidate.missingRules.map((rule, idx) => (
              <li key={idx} className="text-red-600 dark:text-red-400">✗ {rule}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};
