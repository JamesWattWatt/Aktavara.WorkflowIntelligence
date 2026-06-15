import type { AnalyzeResponse } from '../types/api';

interface AnalysisSummaryProps {
  response: AnalyzeResponse | null;
}

export const AnalysisSummary = ({ response }: AnalysisSummaryProps) => {
  if (!response) {
    return null;
  }

  return (
    <div className="p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg space-y-2 text-sm">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <p className="text-gray-500 dark:text-gray-400">File</p>
          <p className="font-medium truncate">{response.fileName}</p>
        </div>
        <div>
          <p className="text-gray-500 dark:text-gray-400">Events</p>
          <p className="font-medium">{response.totalEvents}</p>
        </div>
        <div>
          <p className="text-gray-500 dark:text-gray-400">Candidates</p>
          <p className="font-medium">{response.workflowCandidates.length}</p>
        </div>
        <div>
          <p className="text-gray-500 dark:text-gray-400">Time</p>
          <p className="font-medium">{response.durationMs}ms</p>
        </div>
      </div>
      {response.contextNarrative && (
        <div className="pt-2 border-t border-gray-200 dark:border-gray-700">
          <p className="text-gray-700 dark:text-gray-300 italic text-xs">
            {response.contextNarrative}
          </p>
        </div>
      )}
    </div>
  );
};
