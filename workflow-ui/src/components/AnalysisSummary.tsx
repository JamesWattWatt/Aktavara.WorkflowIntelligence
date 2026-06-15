import { useState } from 'react';
import type { AnalyzeResponse } from '../types/api';
import { HelpIcon } from './HelpIcon';

interface AnalysisSummaryProps {
  response: AnalyzeResponse | null;
  onOpenHelp?: (key: string) => void;
}

const getGuidanceBadgeColor = (level: string): { bg: string; text: string } => {
  switch (level.toLowerCase()) {
    case 'instruct':
      return { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-700 dark:text-green-400' };
    case 'confirm':
      return { bg: 'bg-amber-100 dark:bg-amber-900/30', text: 'text-amber-700 dark:text-amber-400' };
    case 'suggest':
      return { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-700 dark:text-blue-400' };
    default:
      return { bg: 'bg-gray-100 dark:bg-gray-800', text: 'text-gray-600 dark:text-gray-400' };
  }
};

export const AnalysisSummary = ({ response, onOpenHelp }: AnalysisSummaryProps) => {
  const [isExpanded, setIsExpanded] = useState(true);

  if (!response) {
    return null;
  }

  const guidanceBadge = getGuidanceBadgeColor(response.guidanceLevel);

  return (
    <div className="bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
      <div
        onClick={() => setIsExpanded(!isExpanded)}
        className="p-4 cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
      >
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-2">
              <span className="text-lg">{isExpanded ? '▼' : '▶'}</span>
              <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Analysis</p>
              {onOpenHelp && <HelpIcon helpKey="discovery-analysis" onOpen={onOpenHelp} />}
            </div>
            <p className="font-medium text-sm truncate">{response.fileName}</p>
          </div>
          <div className={`px-2 py-1 rounded text-xs font-semibold ${guidanceBadge.bg} ${guidanceBadge.text}`}>
            {response.guidanceLevel}
          </div>
        </div>
      </div>

      {isExpanded && (
        <>
          <div className="border-t border-gray-200 dark:border-gray-700 p-4 space-y-3">
            {/* Key metrics */}
            <div className="grid grid-cols-3 gap-2 text-xs">
              <div>
                <p className="text-gray-500 dark:text-gray-400">Events</p>
                <p className="font-semibold">{response.totalEvents}</p>
              </div>
              <div>
                <p className="text-gray-500 dark:text-gray-400">Candidates</p>
                <p className="font-semibold">{response.workflowCandidates.length}</p>
              </div>
              <div>
                <p className="text-gray-500 dark:text-gray-400">Time</p>
                <p className="font-semibold">{response.durationMs}ms</p>
              </div>
            </div>

            {/* Recommended next step */}
            {response.recommendedNextStep && (
              <div className="p-2 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded text-xs">
                <p className="text-blue-600 dark:text-blue-400 font-semibold">Next Step</p>
                <p className="text-blue-700 dark:text-blue-300 mt-1">{response.recommendedNextStep}</p>
              </div>
            )}

            {/* Context narrative */}
            {response.contextNarrative && (
              <div className="p-2 bg-gray-100 dark:bg-gray-900 rounded border border-gray-300 dark:border-gray-600">
                <p className="text-gray-700 dark:text-gray-300 text-xs leading-relaxed">
                  "{response.contextNarrative}"
                </p>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};
