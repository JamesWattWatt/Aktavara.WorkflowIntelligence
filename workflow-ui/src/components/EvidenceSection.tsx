import { useState } from 'react';
import type { WorkflowCandidateResult } from '../types/api';

interface EvidenceSectionProps {
  candidate: WorkflowCandidateResult;
}

export function EvidenceSection({ candidate }: EvidenceSectionProps) {
  const [isExpanded, setIsExpanded] = useState(true);

  const matchedRulesCount = candidate.matchedRules?.length || 0;
  const totalRulesCount = (candidate.matchedRules?.length || 0) + (candidate.missingRules?.length || 0);

  return (
    <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-gray-50 dark:bg-gray-800/50">
      {/* Collapsible Header */}
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full px-4 py-3 flex items-center justify-between hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors border-b border-gray-200 dark:border-gray-700"
      >
        <div className="flex items-center gap-2">
          <span className="text-lg">{isExpanded ? '▼' : '▶'}</span>
          <p className="text-sm font-semibold text-gray-900 dark:text-gray-100">Evidence & Score</p>
        </div>
        <span className="text-xs text-gray-600 dark:text-gray-400">
          {matchedRulesCount} of {totalRulesCount} matched
        </span>
      </button>

      {/* Expanded Content */}
      {isExpanded && (
        <div className="overflow-x-auto">
          <div className="flex gap-3 p-4" style={{ minWidth: 'min-content' }}>
            {/* Card 1 - Matched Evidence */}
            <div className="flex-shrink-0 w-[200px] p-3 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg">
              <p className="text-xs font-semibold text-gray-600 dark:text-gray-400 mb-2">MATCHED EVIDENCE</p>
              <div className="space-y-1">
                {candidate.matchedEvidence?.slice(0, 5).map((evidence, idx) => (
                  <div key={idx} className="inline-block">
                    <span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 text-xs rounded-full whitespace-nowrap overflow-hidden text-ellipsis block">
                      {evidence}
                    </span>
                  </div>
                ))}
                {(candidate.matchedEvidence?.length || 0) > 5 && (
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    +{(candidate.matchedEvidence?.length || 0) - 5} more
                  </p>
                )}
              </div>
            </div>

            {/* Card 2 - Matched Rules */}
            <div className="flex-shrink-0 w-[200px] p-3 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg">
              <p className="text-xs font-semibold text-gray-600 dark:text-gray-400 mb-2">MATCHED RULES</p>
              <ul className="space-y-1 text-xs">
                {candidate.matchedRules?.slice(0, 5).map((rule, idx) => (
                  <li key={idx} className="flex items-start gap-2 text-gray-700 dark:text-gray-300">
                    <span className="text-green-600 dark:text-green-400 flex-shrink-0">✓</span>
                    <span className="line-clamp-2">{rule}</span>
                  </li>
                ))}
                {(candidate.matchedRules?.length || 0) > 5 && (
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    +{(candidate.matchedRules?.length || 0) - 5} more
                  </p>
                )}
              </ul>
            </div>

            {/* Card 3 - Score Breakdown */}
            <div className="flex-shrink-0 w-[200px] p-3 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg">
              <p className="text-xs font-semibold text-gray-600 dark:text-gray-400 mb-2">SCORE</p>
              <div className="space-y-1 text-xs">
                {Object.entries(candidate.scoreBreakdown || {}).slice(0, 5).map(([key, value]) => (
                  <div key={key} className="flex justify-between">
                    <span className="text-gray-600 dark:text-gray-400">{key}</span>
                    <span className={value > 0 ? 'text-green-600 dark:text-green-400 font-semibold' : 'text-red-600 dark:text-red-400 font-semibold'}>
                      {value.toFixed(2)}
                    </span>
                  </div>
                ))}
              </div>
            </div>

            {/* Card 4 - Next Step */}
            <div className="flex-shrink-0 w-[200px] p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg">
              <p className="text-xs font-semibold text-amber-600 dark:text-amber-400 mb-2">NEXT STEP</p>
              {candidate.nextStepHint ? (
                <>
                  <p className="text-sm font-semibold text-amber-900 dark:text-amber-100 mb-1">
                    → {candidate.nextStepHint}
                  </p>
                  {candidate.currentStateName && (
                    <p className="text-xs text-amber-700 dark:text-amber-300">
                      Based on: {candidate.currentStateName}
                    </p>
                  )}
                </>
              ) : (
                <p className="text-xs text-amber-700 dark:text-amber-300">No next step available</p>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
