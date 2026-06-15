import { useState } from 'react';
import type { AnalyzeResponse, WorkflowCandidateResult } from './types/api';
import { LogDropZone } from './components/LogDropZone';
import { WorkflowList } from './components/WorkflowList';
import { WorkflowDetail } from './components/WorkflowDetail';
import { FlowVisualiser } from './components/FlowVisualiser';
import { WorkshopPanel } from './components/WorkshopPanel';
import { AnalysisSummary } from './components/AnalysisSummary';
import { LibraryPage } from './components/LibraryPage';

export function App() {
  const [topLevelTab, setTopLevelTab] = useState<'discovery' | 'library'>('discovery');
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
    <div className="min-h-screen w-full min-w-[1200px] flex flex-col bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
      {/* Fixed Header - Full width */}
      <header className="sticky top-0 z-20 w-full border-b border-gray-200 dark:border-gray-800 px-6 py-4 bg-white dark:bg-gray-900">
        <div className="flex items-center justify-between mb-3">
          <div>
            <h1 className="text-2xl font-bold">Aktavara Workflow Intelligence</h1>
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
              {topLevelTab === 'discovery' ? 'Discovery & Workshop Interface' : 'Workflow Library Management'}
            </p>
          </div>
        </div>

        {/* Top-level tabs */}
        <div className="flex gap-4 border-b border-gray-200 dark:border-gray-800 -mx-6 px-6">
          <button
            onClick={() => setTopLevelTab('discovery')}
            className={`px-4 py-2 font-medium text-sm border-b-2 transition-colors ${
              topLevelTab === 'discovery'
                ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
            }`}
          >
            Discovery
          </button>
          <button
            onClick={() => setTopLevelTab('library')}
            className={`px-4 py-2 font-medium text-sm border-b-2 transition-colors ${
              topLevelTab === 'library'
                ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
            }`}
          >
            Library
          </button>
        </div>
      </header>

      {/* Main Content */}
      {topLevelTab === 'discovery' ? (
        <div className="w-full flex flex-1 gap-6 p-6 min-h-0">
          {/* Left Sidebar - Fixed 280px */}
          <div className="w-[280px] flex-shrink-0 flex flex-col gap-4 min-h-0">
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

          {/* Middle Column - Flow Visualiser (flex-1 fills remaining space) */}
          <div className="flex-1 flex flex-col min-h-0">
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
              <div className="border border-gray-200 dark:border-gray-800 rounded-lg min-h-0 overflow-y-auto flex flex-col">
                <div className="flex-shrink-0 px-4 py-3 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
                  <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100">Detected Steps</h3>
                </div>
                <div className="flex-1 min-h-0 overflow-y-auto">
                  <FlowVisualiser candidate={selectedCandidate} />
                </div>
              </div>
            ) : (
              <div className="flex items-center justify-center border border-gray-200 dark:border-gray-800 rounded-lg p-6 min-h-[300px]">
                <div className="text-center text-gray-500 dark:text-gray-400">
                  <p className="text-lg font-medium">Select a workflow to view</p>
                  <p className="text-sm mt-2">Choose from the candidates on the left</p>
                </div>
              </div>
            )}
          </div>

          {/* Right Column - Details/Workshop with tabs and sticky header */}
          <div className="w-[380px] flex-shrink-0 flex flex-col min-h-0 border border-gray-200 dark:border-gray-800 rounded-lg overflow-hidden">
            {!analyzeResponse || analyzeResponse.workflowCandidates.length === 0 ? (
              <div className="flex items-center justify-center flex-1 p-6">
                <div className="text-center text-gray-500 dark:text-gray-400">
                  <p className="text-sm font-medium">No workflow selected</p>
                </div>
              </div>
            ) : selectedCandidate ? (
              <>
                {/* Sticky Header - Workflow Info */}
                <div className="flex-shrink-0 sticky top-0 z-10 px-4 py-3 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
                  <div className="mb-2">
                    <h2 className="text-base font-bold text-gray-900 dark:text-gray-100">
                      {selectedCandidate.workflowName}
                    </h2>
                  </div>
                  <div className="flex items-center gap-2 text-xs">
                    <span className="font-semibold text-green-600 dark:text-green-400">
                      {(selectedCandidate.confidenceScore * 100).toFixed(0)}%
                    </span>
                    <span className={`px-2 py-0.5 rounded-full font-medium ${
                      selectedCandidate.confidenceLevel === 'High' ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' :
                      selectedCandidate.confidenceLevel === 'Medium' ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400' :
                      'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400'
                    }`}>
                      {selectedCandidate.confidenceLevel}
                    </span>
                    {selectedCandidate.currentStateName && (
                      <span className="text-gray-500 dark:text-gray-400">
                        {selectedCandidate.currentStateName}
                      </span>
                    )}
                  </div>
                </div>

                {/* Tab Bar */}
                <div className="flex-shrink-0 flex gap-2 border-b border-gray-200 dark:border-gray-700 px-4">
                  <button
                    onClick={() => setActiveTab('detail')}
                    className={`py-2 font-medium text-sm border-b-2 transition-colors ${
                      activeTab === 'detail'
                        ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                        : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
                    }`}
                  >
                    Details
                  </button>
                  <button
                    onClick={() => setActiveTab('workshop')}
                    className={`py-2 font-medium text-sm border-b-2 transition-colors ${
                      activeTab === 'workshop'
                        ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                        : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
                    }`}
                  >
                    Workshop
                  </button>
                </div>

                {/* Tab Content */}
                <div className="flex-1 min-h-0 overflow-y-auto">
                  {activeTab === 'detail' ? (
                    <WorkflowDetail candidate={selectedCandidate} />
                  ) : (
                    <WorkshopPanel
                      candidate={selectedCandidate}
                      workflowId={selectedCandidate.workflowId}
                    />
                  )}
                </div>
              </>
            ) : (
              <div className="flex items-center justify-center flex-1 p-6">
                <div className="text-center text-gray-500 dark:text-gray-400">
                  <p className="text-sm font-medium">Select a workflow to view</p>
                </div>
              </div>
            )}
          </div>
        </div>
      ) : (
        <div className="flex-1 min-h-0">
          <LibraryPage />
        </div>
      )}
    </div>
  );
}

export default App;
