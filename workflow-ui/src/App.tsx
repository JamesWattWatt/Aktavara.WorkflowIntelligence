import { useState } from 'react';
import type { AnalyzeResponse, WorkflowCandidateResult } from './types/api';
import { LogDropZone } from './components/LogDropZone';
import { WorkflowList } from './components/WorkflowList';
import { WorkflowDetail } from './components/WorkflowDetail';
import { FlowVisualiser } from './components/FlowVisualiser';
import { WorkshopPanel } from './components/WorkshopPanel';
import { AnalysisSummary } from './components/AnalysisSummary';

export function App() {
  const [analyzeResponse, setAnalyzeResponse] = useState<AnalyzeResponse | null>(null);
  const [selectedCandidate, setSelectedCandidate] = useState<WorkflowCandidateResult | null>(null);
  const [activeTab, setActiveTab] = useState<'detail' | 'workshop'>('detail');
  const [error, setError] = useState<string | null>(null);

  const handleAnalyzeResult = (response: AnalyzeResponse) => {
    setAnalyzeResponse(response);
    if (response.workflowCandidates.length > 0) {
      setSelectedCandidate(response.workflowCandidates[0]);
    }
    setError(null);
  };

  const handleError = (error: Error) => {
    setError(error.message);
  };

  return (
    <div className="min-h-screen flex flex-col bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
      {/* Fixed Header */}
      <header className="sticky top-0 z-10 border-b border-gray-200 dark:border-gray-800 px-6 py-4 bg-white dark:bg-gray-900">
        <h1 className="text-2xl font-bold">Aktavara Workflow Intelligence</h1>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">Discovery & Workshop Interface</p>
      </header>

      {/* Main Content - Scrollable */}
      <div className="flex flex-1 gap-6 p-6 min-h-0">
        {/* Left Sidebar - Scrollable */}
        <div className="w-80 flex flex-col gap-4 min-h-0">
          <div className="flex-shrink-0">
            <LogDropZone onResult={handleAnalyzeResult} onError={handleError} />
          </div>

          {error && (
            <div className="flex-shrink-0 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-sm text-red-600 dark:text-red-400">
              {error}
            </div>
          )}

          {analyzeResponse && (
            <div className="flex-shrink-0">
              <AnalysisSummary response={analyzeResponse} />
            </div>
          )}

          {/* Scrollable candidate list */}
          <div className="flex-1 min-h-0 border border-gray-200 dark:border-gray-800 rounded-lg overflow-y-auto">
            <WorkflowList
              candidates={analyzeResponse?.workflowCandidates || []}
              selectedId={selectedCandidate?.workflowId}
              onSelect={setSelectedCandidate}
            />
          </div>
        </div>

        {/* Right Main Content - Scrollable */}
        <div className="flex-1 flex flex-col gap-4 min-h-0">
          {!analyzeResponse ? (
            <div className="flex items-center justify-center border border-gray-200 dark:border-gray-800 rounded-lg p-6 min-h-[300px]">
              <div className="text-center text-gray-500 dark:text-gray-400">
                <p className="text-lg font-medium">Drop a log file to begin</p>
                <p className="text-sm mt-2">Discover workflow patterns in your activity logs</p>
              </div>
            </div>
          ) : analyzeResponse.workflowCandidates.length === 0 ? (
            <div className="flex items-center justify-center border border-gray-200 dark:border-gray-800 rounded-lg p-6 min-h-[300px]">
              <div className="text-center text-gray-500 dark:text-gray-400">
                <p className="text-lg font-medium">No workflow patterns detected</p>
                <p className="text-sm mt-2">This log file doesn't match any known workflows</p>
              </div>
            </div>
          ) : selectedCandidate ? (
            <>
              {/* Tabs */}
              <div className="flex-shrink-0 flex gap-2 border-b border-gray-200 dark:border-gray-800">
                <button
                  onClick={() => setActiveTab('detail')}
                  className={`px-4 py-2 font-medium text-sm border-b-2 transition-colors ${
                    activeTab === 'detail'
                      ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                      : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
                  }`}
                >
                  Workflow Details
                </button>
                <button
                  onClick={() => setActiveTab('workshop')}
                  className={`px-4 py-2 font-medium text-sm border-b-2 transition-colors ${
                    activeTab === 'workshop'
                      ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                      : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
                  }`}
                >
                  Workshop
                </button>
              </div>

              {/* Tab Content - Two column grid, scrollable */}
              <div className="flex-1 min-h-0 grid grid-cols-2 gap-4">
                {/* Left: Flow Visualiser */}
                <div className="border border-gray-200 dark:border-gray-800 rounded-lg min-h-0 overflow-y-auto">
                  <FlowVisualiser candidate={selectedCandidate} />
                </div>

                {/* Right: Details or Workshop */}
                <div className="border border-gray-200 dark:border-gray-800 rounded-lg min-h-0 overflow-y-auto">
                  {activeTab === 'detail' ? (
                    <WorkflowDetail candidate={selectedCandidate} />
                  ) : (
                    <WorkshopPanel
                      candidate={selectedCandidate}
                      workflowId={selectedCandidate.workflowId}
                    />
                  )}
                </div>
              </div>
            </>
          ) : (
            <div className="flex items-center justify-center border border-gray-200 dark:border-gray-800 rounded-lg p-6 min-h-[300px]">
              <div className="text-center text-gray-500 dark:text-gray-400">
                <p className="text-lg font-medium">Select a workflow to view</p>
                <p className="text-sm mt-2">Choose from the candidates on the left</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default App;
