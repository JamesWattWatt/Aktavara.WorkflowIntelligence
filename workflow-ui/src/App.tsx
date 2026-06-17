import { useState, useRef } from 'react';
import type { AnalyzeResponse, WorkflowCandidateResult } from './types/api';
import { LogDropZone } from './components/LogDropZone';
import { WorkflowList } from './components/WorkflowList';
import { FlowVisualiser } from './components/FlowVisualiser';
import { WorkshopPanel } from './components/WorkshopPanel';
import { AnalysisSummary } from './components/AnalysisSummary';
import { LibraryPage } from './components/LibraryPage';
import { HelpIcon } from './components/HelpIcon';
import { HelpPanel } from './components/HelpPanel';
import { EvidenceSection } from './components/EvidenceSection';
import { ChatPanel } from './components/ChatPanel';
import { helpContent } from './help/helpContent';
import enghouseLogo from './assets/enghouse-logo.svg';

export function App() {
  const [topLevelTab, setTopLevelTab] = useState<'discovery' | 'library'>('discovery');
  const [analyzeResponse, setAnalyzeResponse] = useState<AnalyzeResponse | null>(null);
  const [selectedCandidate, setSelectedCandidate] = useState<WorkflowCandidateResult | null>(null);
  const [detectedUser, setDetectedUser] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [helpPanelOpen, setHelpPanelOpen] = useState(false);
  const [helpPanelKey, setHelpPanelKey] = useState<string | null>(null);
  const [chatSessionId, setChatSessionId] = useState<string | null>(null);
  const [chatPanelOpen, setChatPanelOpen] = useState(false);
  const contentScrollRef = useRef<HTMLDivElement>(null);

  const toggleChatPanel = (e: React.MouseEvent) => {
    e.preventDefault();
    const scrollTop = contentScrollRef.current?.scrollTop;
    setChatPanelOpen(prev => !prev);
    // Wait for layout reflow to complete before restoring scroll
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        if (contentScrollRef.current && scrollTop !== undefined) {
          contentScrollRef.current.scrollTop = scrollTop;
        }
      });
    });
  };

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
    setDetectedUser(response.detectedUser || null);
    if (response.workflowCandidates.length > 0) {
      setSelectedCandidate(response.workflowCandidates[0]);
    }
    setError(null);
    setChatSessionId(null);
    setChatPanelOpen(true);
  };

  const handleError = (error: Error) => {
    setError(error.message);
  };

  return (
    <div className="min-h-screen w-full min-w-[1200px] flex flex-col bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
      {/* NRM-Style Header - Brand Primary Blue, 48px */}
      <header className="sticky top-0 z-20 w-full h-12 flex items-center px-4 gap-4" style={{ backgroundColor: '#2E75D1' }}>
        {/* Logo */}
        <img
          src={enghouseLogo}
          alt="Enghouse"
          style={{
            height: '28px',
            width: 'auto',
            filter: 'brightness(0) invert(1)'
          }}
          className="flex-shrink-0"
        />

        {/* Divider */}
        <div style={{ width: '1px', height: '24px', backgroundColor: 'rgba(255,255,255,0.2)' }} />

        {/* App Name */}
        <span className="text-sm font-medium text-white flex-shrink-0">Workflow Intelligence</span>

        {/* Navigation Items - Left aligned after app name */}
        <div className="flex items-center gap-0 ml-8">
          <button
            onClick={() => setTopLevelTab('discovery')}
            className={`h-12 px-4 text-xs font-medium transition-colors flex items-center rounded gap-1.5 ${
              topLevelTab === 'discovery'
                ? 'text-white'
                : 'text-white/75 hover:text-white hover:bg-white/10'
            }`}
            title="Workflow Discovery tab"
          >
            <i className="ti ti-radar-2" aria-hidden="true" style={{ fontSize: 'calc(1em + 2px)' }} />
            <span className={topLevelTab === 'discovery' ? 'underline decoration-white decoration-2 underline-offset-4' : ''}>
              Workflow Discovery
            </span>
          </button>
          <button
            onClick={() => setTopLevelTab('library')}
            className={`h-12 px-4 text-xs font-medium transition-colors flex items-center rounded gap-1.5 ${
              topLevelTab === 'library'
                ? 'text-white'
                : 'text-white/75 hover:text-white hover:bg-white/10'
            }`}
            title="Workflow Library tab"
          >
            <i className="ti ti-books" aria-hidden="true" style={{ fontSize: 'calc(1em + 2px)' }} />
            <span className={topLevelTab === 'library' ? 'underline decoration-white decoration-2 underline-offset-4' : ''}>
              Workflow Library
            </span>
          </button>
        </div>

        {/* Right-side Controls */}
        <div className="flex items-center gap-2 flex-shrink-0 ml-auto">
          {/* Help Icon */}
          <button
            onClick={() => openHelp(topLevelTab === 'discovery' ? 'discovery-concept' : 'library-concept')}
            className="h-8 w-8 flex items-center justify-center rounded transition-colors"
            style={{
              color: '#ffffff',
              backgroundColor: 'rgba(255,255,255,0.1)',
              fontSize: '16px'
            }}
            onMouseEnter={(e) => e.currentTarget.style.backgroundColor = 'rgba(255,255,255,0.2)'}
            onMouseLeave={(e) => e.currentTarget.style.backgroundColor = 'rgba(255,255,255,0.1)'}
            title="Help"
          >
            ?
          </button>

          {/* Chat Button (Discovery tab only) */}
          {topLevelTab === 'discovery' && analyzeResponse && (
            <button
              type="button"
              onClick={toggleChatPanel}
              className="h-8 px-3 rounded flex items-center gap-2 font-medium transition-colors text-white text-xs"
              style={{
                backgroundColor: chatPanelOpen ? 'rgba(255,255,255,0.25)' : 'rgba(255,255,255,0.15)'
              }}
              onMouseEnter={(e) => e.currentTarget.style.backgroundColor = 'rgba(255,255,255,0.25)'}
              onMouseLeave={(e) => e.currentTarget.style.backgroundColor = chatPanelOpen ? 'rgba(255,255,255,0.25)' : 'rgba(255,255,255,0.15)'}
              title={chatPanelOpen ? 'Close chat panel' : 'Open chat panel'}
            >
              <span>💬</span>
              <span>Chat</span>
            </button>
          )}
        </div>
      </header>

      {/* Main Content - Starts immediately below header */}
      {topLevelTab === 'discovery' ? (
        <div className={`w-full flex flex-1 gap-6 min-h-0 px-6 py-6 overflow-hidden ${chatPanelOpen ? 'flex-row' : 'flex-row'}`}>
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

          {/* Middle Column - Content area (flex-1 when chat open) */}
          <div
            ref={contentScrollRef}
            className={chatPanelOpen ? 'flex-1 flex flex-col gap-4 min-h-0 overflow-y-auto' : 'flex-1 flex flex-col gap-4 min-h-0 overflow-y-auto'}
            style={{ overflowAnchor: 'none' }}
          >
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
              <div className="relative">

                {/* Framed column 2: all three sections in one container */}
                <div className="flex-1 flex flex-col min-h-0 overflow-hidden border border-gray-200 dark:border-gray-700 rounded-lg bg-gray-50/50 dark:bg-gray-800/30 dark:text-gray-100">
                  {/* Section 1 - Workflow name + subtitle + steps */}
                  <div className="p-4 border-b border-gray-200 dark:border-gray-700">
                    <div className="flex items-center gap-2 mb-2">
                      <h2 className="text-base font-bold">
                        {selectedCandidate.workflowName}
                      </h2>
                      <HelpIcon helpKey="discovery-workflow-details" onOpen={openHelp} />
                    </div>
                    {/* Subtitle line with confidence, state, and next step */}
                    <div className="flex items-center gap-3 text-xs mb-3 flex-wrap">
                      <span className="font-semibold text-green-600 dark:text-green-400">
                        {(selectedCandidate.confidenceScore * 100).toFixed(0)}%
                      </span>
                      <span className={`px-2 py-0.5 rounded-full font-medium ${
                        selectedCandidate.confidenceLevel === 'High' ? 'bg-[#43A047]/10 text-[#43A047]' :
                        selectedCandidate.confidenceLevel === 'Medium' ? 'bg-[#FB8C00]/10 text-[#FB8C00]' :
                        'bg-[#E22A11]/10 text-[#E22A11]'
                      }`}>
                        {selectedCandidate.confidenceLevel}
                      </span>
                      <span className="text-gray-600 dark:text-gray-400">|</span>
                      {selectedCandidate.currentStateName && (
                        <>
                          <span className="text-blue-600 dark:text-blue-400">
                            Current state: {selectedCandidate.currentStateName}
                          </span>
                          <span className="text-gray-600 dark:text-gray-400">|</span>
                        </>
                      )}
                      {selectedCandidate.nextStepHint && (
                        <span className="text-amber-600 dark:text-amber-400">
                          → {selectedCandidate.nextStepHint}
                        </span>
                      )}
                    </div>
                    <p className="text-xs font-semibold text-gray-600 dark:text-gray-400 mb-2">DETECTED STEPS</p>
                    <FlowVisualiser candidate={selectedCandidate} />
                  </div>

                  {/* Section 2 - Evidence & Score (Collapsible, default collapsed) */}
                  <EvidenceSection candidate={selectedCandidate} />

                  {/* Section 3 - Workshop (always visible, no tabs) */}
                  <div className="flex-1 min-h-0 flex flex-col overflow-y-auto">
                    <WorkshopPanel
                      candidate={selectedCandidate}
                      workflowId={selectedCandidate.workflowId}
                      detectedUser={detectedUser}
                      onOpenHelp={openHelp}
                    />
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Right Column - Chat Panel (resizable, slides in) */}
          {chatPanelOpen && (
            <ChatPanel
              sessionId={chatSessionId}
              analyzeResponse={analyzeResponse}
              logFileName={analyzeResponse?.fileName || null}
              onSessionCreated={setChatSessionId}
            />
          )}
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
