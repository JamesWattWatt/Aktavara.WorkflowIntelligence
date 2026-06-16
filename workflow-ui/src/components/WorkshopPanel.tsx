import { useState, useEffect, useRef } from 'react';
import type { WorkflowCandidateResult, HelpGuideSection, GuideSuggestion } from '../types/api';
import { apiClient } from '../services/apiClient';
import { HelpIcon } from './HelpIcon';

interface WorkshopPanelProps {
  candidate: WorkflowCandidateResult | null;
  workflowId?: string;
  onOpenHelp?: (key: string) => void;
}

interface ExportSession {
  exportedAt: string;
  workflowId: string;
  workflowName: string;
  detectedName: string;
  customerName: string;
  confidenceScore: number;
  confidenceLevel: string;
  executionDecision: string;
  matchedRules: string[];
  missingRules: string[];
  questions: {
    question: string;
    answered: boolean;
    notes: string;
  }[];
  sessionNotes: string;
  approvedAt: string | null;
  approvedBy: string | null;
}

export const WorkshopPanel = ({ candidate, workflowId, onOpenHelp }: WorkshopPanelProps) => {
  const [workflowName, setWorkflowName] = useState(candidate?.workflowName || '');
  const [originalName, setOriginalName] = useState(candidate?.workflowName || '');
  const [nameUnsaved, setNameUnsaved] = useState(false);
  const [answerStatus, setAnswerStatus] = useState<Record<number, boolean>>({});
  const [questionNotes, setQuestionNotes] = useState<Record<number, string>>({});
  const [executionDecision, setExecutionDecision] = useState('Guidance only');
  const [currentStatus, setCurrentStatus] = useState<string | null>(null);
  const [customerName, setCustomerName] = useState('');
  const [sessionNotes, setSessionNotes] = useState('');
  const [helpGuide, setHelpGuide] = useState<HelpGuideSection | null>(null);
  const [loadingGuide, setLoadingGuide] = useState(false);
  const [savedMessage, setSavedMessage] = useState<string | null>(null);
  const [showDeprecateConfirm, setShowDeprecateConfirm] = useState(false);
  const [guideSuggestion, setGuideSuggestion] = useState<GuideSuggestion | null>(null);
  const [suggestingGuide, setSuggestingGuide] = useState(false);
  const [showSuggestionUI, setShowSuggestionUI] = useState(false);
  const [generatingQuestions, setGeneratingQuestions] = useState(false);
  const generatedRef = useRef<string | null>(null);

  useEffect(() => {
    if (candidate) {
      setWorkflowName(candidate.workflowName);
      setOriginalName(candidate.workflowName);
      // Fetch current workflow status
      fetchWorkflowStatus();
      // Fetch help guide if available
      if (candidate.nextStepHint && candidate.currentStateName) {
        fetchHelpGuide();
      }
      // Auto-generate questions if empty (once per session)
      if ((!candidate.workshopQuestions || candidate.workshopQuestions.length === 0) && generatedRef.current !== candidate.workflowId) {
        generateQuestionsIfEmpty(candidate.workflowId);
        generatedRef.current = candidate.workflowId;
      }
    }
  }, [candidate]);

  const generateQuestionsIfEmpty = async (workflowId: string) => {
    try {
      setGeneratingQuestions(true);
      await apiClient.generateWorkflowQuestions(workflowId);
      // Refresh the workflow to get updated questions
      await apiClient.getWorkflow(workflowId);
      // Note: The candidate object won't be updated directly, but the next time
      // the parent refreshes data, it will have the new questions
    } catch (error) {
      console.error('Error generating questions:', error);
    } finally {
      setGeneratingQuestions(false);
    }
  };

  const fetchWorkflowStatus = async () => {
    if (!candidate?.workflowId) return;
    try {
      const workflow = await apiClient.getWorkflow(candidate.workflowId);
      setCurrentStatus(workflow.status?.toString() || 'Active');
    } catch (error) {
      console.error('Failed to fetch workflow status:', error);
    }
  };

  const normalizeStepId = (stepId: string): string => {
    return stepId
      .toLowerCase()
      .trim()
      .replace(/\s+/g, '_')
      .replace(/-/g, '_');
  };

  const fetchHelpGuide = async () => {
    if (!candidate?.workflowId || !candidate?.currentStateName) return;
    setLoadingGuide(true);
    try {
      const normalizedStepId = normalizeStepId(candidate.currentStateName);
      const sections = await apiClient.getHelpGuideSection(candidate.workflowId, normalizedStepId);
      if (sections.length > 0) {
        setHelpGuide(sections[0]);
      }
    } catch (error) {
      console.error('Failed to fetch help guide:', error);
    } finally {
      setLoadingGuide(false);
    }
  };

  const suggestGuideMapping = async () => {
    if (!candidate?.workflowId || !candidate?.currentStateName) return;
    setSuggestingGuide(true);
    try {
      const suggestion = await apiClient.suggestGuideMapping(
        candidate.workflowId,
        candidate.workflowName,
        candidate.currentStateName,
        candidate.currentStateName,
        candidate.matchedRules || [],
        candidate.matchedEvidence || []
      );
      setGuideSuggestion(suggestion);
      setShowSuggestionUI(true);
    } catch (error) {
      console.error('Failed to suggest guide mapping:', error);
    } finally {
      setSuggestingGuide(false);
    }
  };

  const approveSuggestion = async () => {
    if (!guideSuggestion || !candidate?.workflowId) return;
    try {
      await apiClient.saveGuideMapping(
        guideSuggestion.workflowId,
        guideSuggestion.stepId,
        guideSuggestion.suggestedGuideFile,
        guideSuggestion.suggestedSectionId || undefined
      );
      setShowSuggestionUI(false);
      setGuideSuggestion(null);
      setSavedMessage('Guide mapping saved successfully');
      setTimeout(() => setSavedMessage(null), 3000);
      // Re-fetch the guide to show the approved mapping
      fetchHelpGuide();
    } catch (error) {
      console.error('Failed to save guide mapping:', error);
    }
  };

  const dismissSuggestion = () => {
    setShowSuggestionUI(false);
    setGuideSuggestion(null);
  };

  const handleSaveName = async () => {
    if (!candidate?.workflowId) return;
    try {
      // For now, just show success - actual API endpoint in Prompt 23
      setSavedMessage('Workflow name updated');
      setOriginalName(workflowName);
      setNameUnsaved(false);
      setTimeout(() => setSavedMessage(null), 3000);
    } catch (error) {
      console.error('Failed to save name:', error);
    }
  };

  const handleStatusChange = async (status: 'Approved' | 'Candidate' | 'Deprecated') => {
    if (!candidate?.workflowId) return;

    if (status === 'Deprecated' && !showDeprecateConfirm) {
      setShowDeprecateConfirm(true);
      return;
    }

    try {
      await apiClient.updateWorkflowStatus(candidate.workflowId, status);
      setCurrentStatus(status);
      setSavedMessage(`Workflow ${status.toLowerCase()}`);
      setShowDeprecateConfirm(false);
      setTimeout(() => setSavedMessage(null), 3000);
    } catch (error) {
      console.error('Failed to update status:', error);
    }
  };

  const handleExportSession = () => {
    if (!candidate) return;

    const questions = (candidate.workshopQuestions || []).map((q, idx) => ({
      question: q,
      answered: answerStatus[idx] || false,
      notes: questionNotes[idx] || ''
    }));

    const exportData: ExportSession = {
      exportedAt: new Date().toISOString(),
      workflowId: candidate.workflowId,
      workflowName: candidate.workflowName,
      detectedName: workflowName,
      customerName,
      confidenceScore: candidate.confidenceScore,
      confidenceLevel: candidate.confidenceLevel,
      executionDecision,
      matchedRules: candidate.matchedRules,
      missingRules: candidate.missingRules,
      questions,
      sessionNotes,
      approvedAt: currentStatus === 'Approved' ? new Date().toISOString() : null,
      approvedBy: null
    };

    const dataStr = JSON.stringify(exportData, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${candidate.workflowName.toLowerCase().replace(/\s+/g, '-')}-workshop-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    setSavedMessage('Session exported');
    setTimeout(() => setSavedMessage(null), 3000);
  };

  if (!candidate || !workflowId) {
    return (
      <div className="p-6 text-center text-gray-500 dark:text-gray-400 flex items-center justify-center min-h-[400px]">
        <p>Select a workflow to begin the workshop</p>
      </div>
    );
  }

  const answeredCount = Object.values(answerStatus).filter(Boolean).length;
  const totalQuestions = candidate.workshopQuestions?.length || 0;

  return (
    <div className="p-6 space-y-6">
      {/* Saved message */}
      {savedMessage && (
        <div className="p-3 bg-green-100 dark:bg-green-900/30 border border-green-300 dark:border-green-800 rounded-lg text-sm text-green-700 dark:text-green-400">
          ✓ {savedMessage}
        </div>
      )}

      {/* Section 1 - Workflow name editor */}
      <div className="border-b border-gray-200 dark:border-gray-700 pb-4">
        <h3 className="font-semibold mb-3">Workflow Name</h3>
        <div className="space-y-2">
          <div className="flex gap-2">
            <input
              type="text"
              value={workflowName}
              onChange={(e) => {
                setWorkflowName(e.target.value);
                setNameUnsaved(e.target.value !== originalName);
              }}
              placeholder="Enter the name your customers use for this task"
              className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-sm"
            />
            {nameUnsaved && (
              <button
                onClick={handleSaveName}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-medium transition-colors"
              >
                Save
              </button>
            )}
          </div>
          <div className="text-xs text-gray-500 dark:text-gray-400">
            {workflowName.length} characters
            {nameUnsaved && ' — unsaved changes'}
          </div>
        </div>
      </div>

      {/* Section 2 - Workshop questions */}
      {totalQuestions > 0 ? (
        <div className="border-b border-gray-200 dark:border-gray-700 pb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">Workshop Questions</h3>
            <span className="text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 px-2 py-1 rounded-full">
              {answeredCount} of {totalQuestions}
            </span>
          </div>
          <div className="space-y-3">
            {candidate.workshopQuestions!.map((question, idx) => (
              <div key={idx} className="border border-gray-200 dark:border-gray-700 rounded-lg p-3">
                <label className="flex items-start gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={answerStatus[idx] || false}
                    onChange={(e) => setAnswerStatus({ ...answerStatus, [idx]: e.target.checked })}
                    className="mt-1 flex-shrink-0"
                  />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium">{question}</p>
                  </div>
                </label>
                {answerStatus[idx] && (
                  <textarea
                    value={questionNotes[idx] || ''}
                    onChange={(e) => setQuestionNotes({ ...questionNotes, [idx]: e.target.value })}
                    placeholder="Add notes..."
                    className="w-full mt-2 p-2 border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-800 text-sm"
                    rows={2}
                  />
                )}
              </div>
            ))}
          </div>
        </div>
      ) : generatingQuestions ? (
        <div className="border-b border-gray-200 dark:border-gray-700 pb-4 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg text-sm text-blue-700 dark:text-blue-400">
          <div className="flex items-center gap-2">
            <div className="animate-spin">⏳</div>
            Generating workshop questions...
          </div>
        </div>
      ) : (
        <div className="border-b border-gray-200 dark:border-gray-700 pb-4 p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg text-sm text-gray-600 dark:text-gray-400">
          No workshop questions generated for this workflow yet. Add them in the Library management panel.
        </div>
      )}

      {/* Section 3 - Execution decision (3-column card layout) */}
      <div className="border-b border-gray-200 dark:border-gray-700 pb-4">
        <h3 className="font-semibold mb-3">Execution Decision</h3>
        <div className="flex gap-2">
          {['Guidance only', 'Assisted action', 'Governed execution'].map((option) => (
            <label
              key={option}
              className={`flex-1 p-3 border rounded-lg cursor-pointer transition-all ${
                executionDecision === option
                  ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
                  : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800/50'
              }`}
            >
              <div className="flex flex-col gap-2">
                <div className="flex items-center gap-2">
                  <input
                    type="radio"
                    name="execution"
                    value={option}
                    checked={executionDecision === option}
                    onChange={(e) => setExecutionDecision(e.target.value)}
                    className="flex-shrink-0"
                  />
                  <p className="text-sm font-medium">{option}</p>
                </div>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  {option === 'Guidance only' &&
                    'The assistant provides step-by-step instructions but the user performs all actions manually.'}
                  {option === 'Assisted action' &&
                    'The assistant can perform individual steps on behalf of the user with explicit confirmation at each step.'}
                  {option === 'Governed execution' &&
                    'The full workflow runs automatically with pre-approved governance rules. Requires Temporal.'}
                </p>
                {option === 'Governed execution' && (
                  <div className="inline-block px-2 py-1 bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 text-xs rounded w-fit">
                    ⚠️ Additional implementation required
                  </div>
                )}
              </div>
            </label>
          ))}
        </div>
      </div>

      {/* Section 4 - Status controls */}
      <div className="border-b border-gray-200 dark:border-gray-700 pb-4">
        <div className="flex items-center justify-between mb-3">
          <h3 className="font-semibold">Workflow Status</h3>
          {currentStatus && (
            <span className="text-xs bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 px-2 py-1 rounded-full">
              {currentStatus}
            </span>
          )}
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => handleStatusChange('Approved')}
            className={`flex-1 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
              currentStatus === 'Approved'
                ? 'bg-green-600 hover:bg-green-700 text-white'
                : 'border border-green-300 dark:border-green-700 text-green-700 dark:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/20'
            }`}
          >
            ✓ Approve
          </button>
          <button
            onClick={() => handleStatusChange('Candidate')}
            className={`flex-1 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
              currentStatus === 'Candidate' || !currentStatus
                ? 'bg-blue-600 hover:bg-blue-700 text-white'
                : 'border border-blue-300 dark:border-blue-700 text-blue-700 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20'
            }`}
          >
            Candidate
          </button>
          <button
            onClick={() => handleStatusChange('Deprecated')}
            className="flex-1 px-3 py-2 rounded-lg text-sm font-medium border border-red-300 dark:border-red-700 text-red-700 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
          >
            ✗ Deprecate
          </button>
        </div>

        {/* Deprecate confirmation */}
        {showDeprecateConfirm && (
          <div className="mt-3 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
            <p className="text-sm text-red-700 dark:text-red-400 mb-2">
              Are you sure? This will hide the workflow from the discovery interface.
            </p>
            <div className="flex gap-2">
              <button
                onClick={() => {
                  setShowDeprecateConfirm(false);
                  handleStatusChange('Deprecated');
                }}
                className="flex-1 px-3 py-2 bg-red-600 hover:bg-red-700 text-white rounded text-sm font-medium transition-colors"
              >
                Confirm Deprecate
              </button>
              <button
                onClick={() => setShowDeprecateConfirm(false)}
                className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded text-sm font-medium transition-colors"
              >
                Cancel
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Section 5 - Export workshop session */}
      <div className="border-b border-gray-200 dark:border-gray-700 pb-4">
        <h3 className="font-semibold mb-3">Export Session</h3>
        <div className="space-y-3">
          <input
            type="text"
            value={customerName}
            onChange={(e) => setCustomerName(e.target.value)}
            placeholder="Customer name"
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-sm"
          />
          <textarea
            value={sessionNotes}
            onChange={(e) => setSessionNotes(e.target.value)}
            placeholder="General session notes..."
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-sm"
            rows={3}
          />
          <button
            onClick={handleExportSession}
            className="w-full px-3 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-medium transition-colors"
          >
            ⬇ Export Session JSON
          </button>
        </div>
      </div>

      {/* Section 6 - Relevant documentation header + Help guide preview */}
      <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
        <div className="flex items-center gap-2 mb-3">
          <h3 className="font-semibold">Relevant documentation</h3>
          {onOpenHelp && <HelpIcon helpKey="discovery-workshop-guide" onOpen={onOpenHelp} />}
        </div>
        {loadingGuide ? (
          <div className="p-3 text-sm text-gray-500 dark:text-gray-400">Loading guide...</div>
        ) : helpGuide ? (
          <div className="p-4 bg-gray-50 dark:bg-gray-900/50 rounded-lg">
            <h4 className="font-semibold mb-2 text-sm">{helpGuide.heading}</h4>
            <div className="text-sm text-gray-700 dark:text-gray-300 whitespace-pre-wrap">
              {helpGuide.content}
            </div>
          </div>
        ) : candidate.nextStepHint ? (
        <div className="space-y-3">
          {showSuggestionUI && guideSuggestion ? (
            <div className="p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
              <h4 className="font-semibold mb-2 text-sm text-blue-900 dark:text-blue-300">
                Suggested Guide
              </h4>
              <div className="space-y-2 mb-3">
                <p className="text-sm text-blue-800 dark:text-blue-400">
                  <span className="font-medium">File:</span> {guideSuggestion.suggestedGuideFile}
                </p>
                <p className="text-sm text-blue-800 dark:text-blue-400">
                  <span className="font-medium">Section:</span> {guideSuggestion.suggestedSectionHeading}
                </p>
                <p className="text-sm text-blue-800 dark:text-blue-400">
                  <span className="font-medium">Reason:</span> {guideSuggestion.confidenceReason}
                </p>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={approveSuggestion}
                  className="flex-1 px-3 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded text-sm font-medium transition-colors"
                >
                  ✓ Approve
                </button>
                <button
                  onClick={dismissSuggestion}
                  className="flex-1 px-3 py-2 border border-blue-300 dark:border-blue-700 text-blue-700 dark:text-blue-400 rounded text-sm font-medium hover:bg-blue-50 dark:hover:bg-blue-900/30 transition-colors"
                >
                  ✗ Dismiss
                </button>
              </div>
            </div>
          ) : (
            <button
              onClick={suggestGuideMapping}
              disabled={suggestingGuide}
              className="w-full px-3 py-2 bg-amber-600 hover:bg-amber-700 disabled:opacity-50 disabled:cursor-not-allowed text-white rounded-lg text-sm font-medium transition-colors"
            >
              {suggestingGuide ? 'Suggesting...' : '✨ Suggest Guide'}
            </button>
          )}
        </div>
      ) : null}
      </div>
    </div>
  );
};
