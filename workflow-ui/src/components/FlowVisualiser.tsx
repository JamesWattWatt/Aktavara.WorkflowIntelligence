import type { WorkflowCandidateResult } from '../types/api';

interface FlowVisualiserProps {
  candidate: WorkflowCandidateResult | null;
}

export const FlowVisualiser = ({ candidate }: FlowVisualiserProps) => {
  if (!candidate) {
    return (
      <div className="p-6 text-center text-gray-500 dark:text-gray-400 h-full flex items-center justify-center">
        <p>Select a workflow to see flow visualization</p>
      </div>
    );
  }

  return (
    <div className="p-6 h-full">
      <h3 className="font-semibold mb-4">Workflow Flow</h3>
      <div className="bg-gray-100 dark:bg-gray-800 rounded-lg p-4 h-64 flex items-center justify-center">
        <p className="text-gray-500 dark:text-gray-400">Flow visualization coming in Prompt 21</p>
      </div>
    </div>
  );
};
