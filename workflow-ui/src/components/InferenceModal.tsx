import { useState } from 'react';
import type { InferredWorkflowSuggestion } from '../types/api';
import { apiClient } from '../services/apiClient';

interface InferenceModalProps {
  onClose: () => void;
  onApplySuggestions: (suggestion: InferredWorkflowSuggestion) => void;
}

type Step = 'source' | 'review' | 'apply';

export function InferenceModal({ onClose, onApplySuggestions }: InferenceModalProps) {
  const [step, setStep] = useState<Step>('source');
  const [sourceType, setSourceType] = useState<'paste' | 'file'>('paste');
  const [logContent, setLogContent] = useState('');
  const [logFile, setLogFile] = useState<File | null>(null);
  const [suggestion, setSuggestion] = useState<InferredWorkflowSuggestion | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [suggestedName, setSuggestedName] = useState<any | null>(null);
  const [acceptedSuggestions, setAcceptedSuggestions] = useState<Set<string>>(new Set([
    'name', 'riskLevel', 'tags', 'rules', 'threshold'
  ]));

  async function handleAnalyse() {
    try {
      setLoading(true);
      setError(null);

      const content = sourceType === 'paste' ? logContent : await logFile!.text();
      if (!content.trim()) {
        throw new Error('Please provide log content');
      }

      const result = await apiClient.inferWorkflow(content);
      setSuggestion(result);
      setStep('review');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to analyze');
    } finally {
      setLoading(false);
    }
  }

  async function handleGetNameSuggestion() {
    if (!suggestion) return;

    try {
      setLoading(true);
      const result = await apiClient.inferWorkflowName(suggestion);
      setSuggestedName(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to suggest name');
    } finally {
      setLoading(false);
    }
  }

  function toggleAccepted(key: string) {
    const newSet = new Set(acceptedSuggestions);
    if (newSet.has(key)) {
      newSet.delete(key);
    } else {
      newSet.add(key);
    }
    setAcceptedSuggestions(newSet);
  }

  async function handleApply() {
    if (!suggestion) return;
    onApplySuggestions(suggestion);
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white dark:bg-gray-800 rounded-lg max-w-2xl w-full max-h-[90vh] overflow-auto">
        {/* Header */}
        <div className="sticky top-0 flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
          <h2 className="text-lg font-bold">
            {step === 'source' && 'Infer from Logs'}
            {step === 'review' && 'Review Suggestions'}
            {step === 'apply' && 'Apply Changes'}
          </h2>
          <button
            onClick={onClose}
            className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
          >
            ✕
          </button>
        </div>

        {/* Content */}
        <div className="px-6 py-4">
          {error && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 text-sm rounded-lg">
              {error}
            </div>
          )}

          {step === 'source' && (
            <div className="space-y-4">
              <div className="space-y-3">
                <label className="flex items-center gap-2">
                  <input
                    type="radio"
                    checked={sourceType === 'paste'}
                    onChange={() => setSourceType('paste')}
                  />
                  <span className="font-medium">Paste log content</span>
                </label>
                {sourceType === 'paste' && (
                  <textarea
                    value={logContent}
                    onChange={e => setLogContent(e.target.value)}
                    placeholder="Paste your activity log here..."
                    rows={8}
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 font-mono text-sm"
                  />
                )}
              </div>

              <div className="space-y-3">
                <label className="flex items-center gap-2">
                  <input
                    type="radio"
                    checked={sourceType === 'file'}
                    onChange={() => setSourceType('file')}
                  />
                  <span className="font-medium">Upload log file</span>
                </label>
                {sourceType === 'file' && (
                  <input
                    type="file"
                    accept=".txt"
                    onChange={e => setLogFile(e.target.files?.[0] || null)}
                    className="w-full"
                  />
                )}
              </div>

              <button
                onClick={handleAnalyse}
                disabled={loading || (!logContent.trim() && !logFile)}
                className="w-full px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium"
              >
                {loading ? 'Analysing...' : 'Analyse'}
              </button>
            </div>
          )}

          {step === 'review' && suggestion && (
            <div className="space-y-4">
              <div className="p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg text-sm text-blue-900 dark:text-blue-300">
                Analysis complete — {suggestion.evidenceSessions} sessions, {suggestion.evidenceEvents} events
              </div>

              <div className="space-y-3">
                {/* Name */}
                <div className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg">
                  <div className="flex items-start justify-between mb-2">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <input
                          type="checkbox"
                          checked={acceptedSuggestions.has('name')}
                          onChange={() => toggleAccepted('name')}
                        />
                        <span className="font-medium">Name: {suggestedName?.suggestedName || suggestion.suggestedName}</span>
                      </div>
                      <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">Inferred from activity patterns</p>
                    </div>
                    <button
                      onClick={handleGetNameSuggestion}
                      disabled={loading}
                      className="px-3 py-1 text-xs bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-600 font-medium"
                    >
                      {loading ? 'Suggesting...' : 'Get suggestion'}
                    </button>
                  </div>
                  {suggestedName?.alternativeNames?.length > 0 && (
                    <div className="text-xs text-gray-600 dark:text-gray-400">
                      <p className="font-medium mt-2">Alternatives:</p>
                      <ul className="list-disc list-inside">
                        {suggestedName.alternativeNames.map((name: string) => (
                          <li key={name}>{name}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>

                {/* Risk Level */}
                <div className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg">
                  <div className="flex items-center gap-2 mb-1">
                    <input
                      type="checkbox"
                      checked={acceptedSuggestions.has('riskLevel')}
                      onChange={() => toggleAccepted('riskLevel')}
                    />
                    <span className="font-medium">Risk level: {suggestion.suggestedRiskLevel}</span>
                  </div>
                  <p className="text-xs text-gray-600 dark:text-gray-400">
                    Based on operation types and frequency
                  </p>
                </div>

                {/* Tags */}
                <div className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg">
                  <div className="flex items-start gap-2 mb-1">
                    <input
                      type="checkbox"
                      checked={acceptedSuggestions.has('tags')}
                      onChange={() => toggleAccepted('tags')}
                      className="mt-1"
                    />
                    <div className="flex-1">
                      <span className="font-medium">Tags</span>
                      <div className="flex flex-wrap gap-1 mt-2">
                        {suggestion.suggestedTags.map(tag => (
                          <span key={tag} className="px-2 py-0.5 bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 rounded text-xs">
                            {tag}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>
                  <p className="text-xs text-gray-600 dark:text-gray-400">From RecordKinds found in evidence</p>
                </div>

                {/* Rules */}
                <div className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg">
                  <div className="flex items-center gap-2 mb-2">
                    <input
                      type="checkbox"
                      checked={acceptedSuggestions.has('rules')}
                      onChange={() => toggleAccepted('rules')}
                    />
                    <span className="font-medium">{suggestion.suggestedRules.length} signature rules inferred</span>
                  </div>
                  {acceptedSuggestions.has('rules') && (
                    <div className="text-xs text-gray-600 dark:text-gray-400 space-y-1">
                      {suggestion.suggestedRules.slice(0, 3).map((rule, i) => (
                        <div key={i} className="pl-4">• {rule.description || `Event Type ${rule.eventType}`}</div>
                      ))}
                      {suggestion.suggestedRules.length > 3 && (
                        <div className="pl-4">+ {suggestion.suggestedRules.length - 3} more</div>
                      )}
                    </div>
                  )}
                </div>

                {/* Threshold */}
                <div className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg">
                  <div className="flex items-center gap-2 mb-1">
                    <input
                      type="checkbox"
                      checked={acceptedSuggestions.has('threshold')}
                      onChange={() => toggleAccepted('threshold')}
                    />
                    <span className="font-medium">Confidence threshold: {suggestion.suggestedThreshold.toFixed(2)}</span>
                  </div>
                  <p className="text-xs text-gray-600 dark:text-gray-400">Average confidence - margin</p>
                </div>

                {/* Variants */}
                {suggestion.variants.length > 0 && (
                  <div className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="text-lg">?</span>
                      <span className="font-medium">{suggestion.variants.length} variants detected</span>
                    </div>
                    <div className="text-xs text-gray-600 dark:text-gray-400 space-y-1 pl-6">
                      {suggestion.variants.map((v, i) => (
                        <div key={i}>
                          {v.description} ({v.percentage.toFixed(0)}%)
                        </div>
                      ))}
                    </div>
                    <p className="text-xs text-gray-600 dark:text-gray-400 mt-2">Review these in the States tab</p>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="sticky bottom-0 flex gap-2 px-6 py-4 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
          {step === 'source' && (
            <>
              <button
                onClick={onClose}
                className="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700 font-medium"
              >
                Cancel
              </button>
            </>
          )}
          {step === 'review' && (
            <>
              <button
                onClick={() => {
                  setStep('source');
                  setSuggestion(null);
                  setSuggestedName(null);
                }}
                className="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700 font-medium"
              >
                Back
              </button>
              <button
                onClick={handleApply}
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-medium"
              >
                Apply suggestions
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
