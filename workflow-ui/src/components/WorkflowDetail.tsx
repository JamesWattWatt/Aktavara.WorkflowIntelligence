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
      {/* Title and confidence */}
      <div>
        <h2 className="text-xl font-bold mb-3">{candidate.workflowName}</h2>
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

      {/* Current State */}
      {candidate.currentStateName && (
        <div className="p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
          <p className="text-xs text-blue-600 dark:text-blue-400 font-semibold">CURRENT STATE</p>
          <p className="text-sm font-medium mt-1">{candidate.currentStateName}</p>
        </div>
      )}

      {/* Next Step Hint */}
      {candidate.nextStepHint && (
        <div className="p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg">
          <p className="text-xs text-amber-600 dark:text-amber-400 font-semibold">NEXT STEP</p>
          <p className="text-sm font-medium mt-1">{candidate.nextStepHint}</p>
        </div>
      )}

      {/* Score Breakdown Table */}
      <div>
        <h3 className="font-semibold mb-3 text-sm">Score Breakdown</h3>
        <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden text-sm">
          <table className="w-full">
            <tbody>
              {Object.entries(candidate.scoreBreakdown).map(([key, value]) => (
                <tr key={key} className="border-b border-gray-200 dark:border-gray-700 last:border-b-0">
                  <td className="p-2 text-gray-600 dark:text-gray-400">{key}</td>
                  <td className="p-2 text-right font-semibold text-gray-900 dark:text-gray-100">
                    {value.toFixed(2)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Matched Evidence */}
      {candidate.matchedEvidence.length > 0 && (
        <div>
          <h3 className="font-semibold mb-2 text-sm">Matched Evidence</h3>
          <div className="flex flex-wrap gap-2">
            {candidate.matchedEvidence.map((evidence, idx) => (
              <span
                key={idx}
                className="inline-block px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 text-xs rounded cursor-pointer hover:bg-green-200 dark:hover:bg-green-900/50 transition-colors"
              >
                ✓ {evidence}
              </span>
            ))}
          </div>
        </div>
      )}

      {/* Matched Rules */}
      {candidate.matchedRules.length > 0 && (
        <div>
          <h3 className="font-semibold mb-2 text-sm">Matched Rules</h3>
          <ul className="space-y-1 text-sm">
            {candidate.matchedRules.map((rule, idx) => (
              <li key={idx} className="text-green-600 dark:text-green-400 flex gap-2">
                <span className="flex-shrink-0">✓</span>
                <span>{rule}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Missing Rules */}
      {candidate.missingRules.length > 0 && (
        <div>
          <h3 className="font-semibold mb-2 text-sm">Missing Rules</h3>
          <ul className="space-y-1 text-sm">
            {candidate.missingRules.map((rule, idx) => (
              <li key={idx} className="text-red-600 dark:text-red-400 flex gap-2">
                <span className="flex-shrink-0">✗</span>
                <span>{rule}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Workshop Questions Preview */}
      {candidate.workshopQuestions && candidate.workshopQuestions.length > 0 && (
        <div>
          <h3 className="font-semibold mb-2 text-sm">Workshop Questions</h3>
          <div className="space-y-2">
            {candidate.workshopQuestions.slice(0, 2).map((question, idx) => (
              <div key={idx} className="p-2 bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded text-sm text-purple-900 dark:text-purple-300">
                <p className="text-xs font-semibold text-purple-600 dark:text-purple-400">Q{idx + 1}</p>
                <p className="mt-1">{question}</p>
              </div>
            ))}
            {candidate.workshopQuestions.length > 2 && (
              <p className="text-xs text-gray-500 dark:text-gray-400">
                +{candidate.workshopQuestions.length - 2} more questions in Workshop tab
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
