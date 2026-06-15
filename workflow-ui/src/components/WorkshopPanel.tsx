import type { WorkflowCandidateResult } from '../types/api';

interface WorkshopPanelProps {
  candidate: WorkflowCandidateResult | null;
  workflowId?: string;
}

export const WorkshopPanel = ({ candidate, workflowId }: WorkshopPanelProps) => {
  if (!candidate || !workflowId) {
    return (
      <div className="p-6 text-center text-gray-500 dark:text-gray-400 h-full flex items-center justify-center">
        <p>Select a workflow to see workshop questions</p>
      </div>
    );
  }

  if (!candidate.workshopQuestions || candidate.workshopQuestions.length === 0) {
    return (
      <div className="p-6 text-center text-gray-500 dark:text-gray-400">
        <p>No workshop questions available</p>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-4">
      <h3 className="font-semibold">Workshop Questions</h3>
      <div className="space-y-3">
        {candidate.workshopQuestions.map((question, idx) => (
          <div
            key={idx}
            className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg"
          >
            <p className="text-sm"><strong>Q{idx + 1}:</strong> {question}</p>
            <textarea
              className="w-full mt-2 p-2 border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-800 text-sm"
              placeholder="Your answer..."
              rows={2}
            />
          </div>
        ))}
      </div>
    </div>
  );
};
