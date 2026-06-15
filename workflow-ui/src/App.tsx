import { useState } from 'react';
import type { AnalyzeResponse, WorkflowCandidateResult } from './types/api';
import { LogDropZone } from './components/LogDropZone';
import { WorkflowList } from './components/WorkflowList';
import { WorkflowDetail } from './components/WorkflowDetail';
import { FlowVisualiser } from './components/FlowVisualiser';
import { WorkshopPanel } from './components/WorkshopPanel';
import { AnalysisSummary } from './components/AnalysisSummary';
import { LibraryPage } from './components/LibraryPage';
import { HelpIcon } from './components/HelpIcon';
import { HelpPanel } from './components/HelpPanel';
import { EvidenceSection } from './components/EvidenceSection';
import { helpContent } from './help/helpContent';

export function App() {
  const [topLevelTab, setTopLevelTab] = useState<'discovery' | 'library'>('discovery');
  const [analyzeResponse, setAnalyzeResponse] = useState<AnalyzeResponse | null>(null);
  const [selectedCandidate, setSelectedCandidate] = useState<WorkflowCandidateResult | null>(null);
  const [activeTab, setActiveTab] = useState<'detail' | 'workshop'>('detail');
  const [error, setError] = useState<string | null>(null);
  const [helpPanelOpen, setHelpPanelOpen] = useState(false);
  const [helpPanelKey, setHelpPanelKey] = useState<string | null>(null);

  const openHelp = (key: string) => {
    setHelpPanelKey(key);
    setHelpPanelOpen(true);
  };

  const closeHelp = () => {
    setHelpPanelOpen(false);
  };

  const currentHelpContent = helpPanelKey && helpContent[helpPanelKey]
    ? helpContent[helpPanelKey]
    : { title: '', content: '' };

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
      {/* Fixed Header - Full width, flush left */}
      <header className="sticky top-0 z-20 w-full border-b border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900">
        <div className="flex items-center justify-between mb-3 px-6 py-4">
          <div>
            <h1 className="text-2xl font-bold">Aktavara Workflow Intelligence</h1>
            <div className="flex items-center gap-2 mt-1">
              <p className="text-sm text-gray-500 dark:text-gray-400">
                {topLevelTab === 'discovery' ? 'Discovery & Workshop Interface' : 'Workflow Library Management'}
              </p>
              <HelpIcon
                helpKey={topLevelTab === 'discovery' ? 'discovery-concept' : 'library-concept'}
                onOpen={openHelp}
              />
            </div>
          </div>
        </div>

        {/* Top-level tabs */}
        <div className="flex gap-4 border-b border-gray-200 dark:border-gray-800 px-6">
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

      {/* Main Content - Flush with left edge */}
      {topLevelTab === 'discovery' ? (
        <div className="w-full flex flex-1 gap-6 py-6 min-h-0 px-6">
          {/* Left Column - Fixed 280px */}
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
                <AnalysisSummary response={analyzeResponse} onOpenHelp={openHelp} />
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

          {/* Right Column - Flex-1 fills remaining space */}
          <div className="flex-1 flex flex-col gap-4 min-h-0 overflow-y-auto">
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
            ) : !selectedCandidate ? (
              <div className="flex items-center justify-center border border-gray-200 dark:border-gray-800 rounded-lg p-6 min-h-[300px]">
                <div className="text-center text-gray-500 dark:text-gray-400">
                  <p className="text-lg font-medium">Select a workflow to view</p>
                  <p className="text-sm mt-2">Choose from the candidates on the left</p>
                </div>
              </div>
            ) : (
              <>
                {/* Section 1 - Workflow name + Horizontal steps */}
                <div className="border border-gray-200 dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800/50 p-4">
                  <h2 className="text-base font-bold text-gray-900 dark:text-gray-100 mb-2">
                    {selectedCandidate.workflowName}
                  </h2>
                  <div className="flex items-center gap-2 text-xs mb-3">
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
                      <span className="text-gray-600 dark:text-gray-400">
                        {selectedCandidate.currentStateName}
                      </span>
                    )}
                  </div>
                  <p className="text-xs font-semibold text-gray-600 dark:text-gray-400 mb-2">DETECTED STEPS</p>
                  <FlowVisualiser candidate={selectedCandidate} />
                </div>

                {/* Section 2 - Evidence & Score (Collapsible) */}
                <EvidenceSection candidate={selectedCandidate} />

                {/* Section 3 - Workflow Details / Workshop tabs */}
                <div className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 flex flex-col min-h-0 flex-1 overflow-hidden">
                  {/* Tab Bar */}
                  <div className="flex-shrink-0 flex gap-4 border-b border-gray-200 dark:border-gray-700 px-4">
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => setActiveTab('detail')}
                        className={`py-2 font-medium text-sm border-b-2 transition-colors ${
                          activeTab === 'detail'
                            ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                            : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
                        }`}
                      >
                        Workflow details
                      </button>
                      <HelpIcon helpKey="discovery-workflow-details" onOpen={openHelp} />
                    </div>
                    <div className="flex items-center gap-2">
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
                      <HelpIcon helpKey="discovery-workshop" onOpen={openHelp} />
                    </div>
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
                </div>
              </>
            )}
          </div>
        </div>
      ) : (
        <div className="flex-1 min-h-0">
          <LibraryPage onOpenHelp={openHelp} />
        </div>
      )}

      {/* Help Panel */}
      <HelpPanel
        isOpen={helpPanelOpen}
        onClose={closeHelp}
        title={currentHelpContent.title}
        content={currentHelpContent.content}
      />
    </div>
  );
}

export default App;
