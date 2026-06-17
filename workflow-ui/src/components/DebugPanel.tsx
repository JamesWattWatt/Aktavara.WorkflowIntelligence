import React, { useState } from 'react';
import type { AnalyzeResponse, WorkflowCandidateResult } from '../types/api';

interface DebugPanelProps {
  analyzeResponse: AnalyzeResponse | null;
  selectedCandidate: WorkflowCandidateResult | null;
  systemPrompt?: string;
  inputTokens?: number;
  outputTokens?: number;
  responseTimeMs?: number;
  guideReferences?: string[];
}

export const DebugPanel: React.FC<DebugPanelProps> = ({
  analyzeResponse,
  selectedCandidate,
  systemPrompt = '',
  inputTokens = 0,
  outputTokens = 0,
  responseTimeMs = 0,
  guideReferences = []
}) => {
  const [expandedSections, setExpandedSections] = useState<Record<string, boolean>>({
    activity: true,
    workflow: true,
    systemPrompt: false,
    llmDetails: true,
    guides: false
  });

  const toggleSection = (key: string) => {
    setExpandedSections(prev => ({
      ...prev,
      [key]: !prev[key]
    }));
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
  };

  return (
    <div className="flex flex-col h-full bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
      {/* Header */}
      <div className="px-4 py-3 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 flex-shrink-0">
        <h3 className="font-semibold text-sm text-gray-900 dark:text-gray-100">Debug & Insights</h3>
      </div>

      {/* Sections */}
      <div className="flex-1 overflow-y-auto">
        {/* SECTION 1 - Activity Context */}
        <div className="border-b border-gray-200 dark:border-gray-700">
          <button
            onClick={() => toggleSection('activity')}
            className="w-full px-4 py-3 flex items-center justify-between hover:bg-gray-100 dark:hover:bg-gray-800 transition text-left"
          >
            <span className="font-medium text-sm text-gray-900 dark:text-gray-100">
              Activity Context
            </span>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {expandedSections.activity ? '▼' : '▶'}
            </span>
          </button>
          {expandedSections.activity && (
            <div className="px-4 py-3 space-y-2 text-xs bg-gray-50 dark:bg-gray-800/50 border-t border-gray-200 dark:border-gray-700">
              <div>
                <span className="font-medium text-gray-700 dark:text-gray-300">Detected User:</span>
                <span className="ml-2 text-gray-600 dark:text-gray-400">{analyzeResponse?.detectedUser || 'N/A'}</span>
              </div>
              <div>
                <span className="font-medium text-gray-700 dark:text-gray-300">Total Events:</span>
                <span className="ml-2 text-gray-600 dark:text-gray-400">{analyzeResponse?.totalEvents || 0}</span>
              </div>
              <div>
                <span className="font-medium text-gray-700 dark:text-gray-300">Current State:</span>
                <span className="ml-2 text-gray-600 dark:text-gray-400">{selectedCandidate?.currentStateName || 'N/A'}</span>
              </div>
              <div>
                <span className="font-medium text-gray-700 dark:text-gray-300">Time Window:</span>
                <span className="ml-2 text-gray-600 dark:text-gray-400">{analyzeResponse?.durationMs ? `${(analyzeResponse.durationMs / 1000).toFixed(1)}s` : 'N/A'}</span>
              </div>
              {analyzeResponse?.activeEntities && analyzeResponse.activeEntities.length > 0 && (
                <div>
                  <span className="font-medium text-gray-700 dark:text-gray-300">Active Entities (Top 3):</span>
                  <div className="ml-2 mt-1 space-y-1">
                    {analyzeResponse.activeEntities.slice(0, 3).map((entity, idx) => (
                      <div key={idx} className="text-gray-600 dark:text-gray-400">
                        • {entity.name} ({entity.recordKind})
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        {/* SECTION 2 - Workflow Matching */}
        <div className="border-b border-gray-200 dark:border-gray-700">
          <button
            onClick={() => toggleSection('workflow')}
            className="w-full px-4 py-3 flex items-center justify-between hover:bg-gray-100 dark:hover:bg-gray-800 transition text-left"
          >
            <span className="font-medium text-sm text-gray-900 dark:text-gray-100">
              Workflow Matching
            </span>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {expandedSections.workflow ? '▼' : '▶'}
            </span>
          </button>
          {expandedSections.workflow && (
            <div className="px-4 py-3 space-y-3 text-xs bg-gray-50 dark:bg-gray-800/50 border-t border-gray-200 dark:border-gray-700">
              <div>
                <span className="font-medium text-gray-700 dark:text-gray-300">Matched Workflow:</span>
                <span className="ml-2 text-gray-600 dark:text-gray-400">{selectedCandidate?.workflowName || 'N/A'}</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="font-medium text-gray-700 dark:text-gray-300">Confidence:</span>
                <span className={`font-bold ${
                  selectedCandidate?.confidenceLevel === 'High' ? 'text-green-600 dark:text-green-400' :
                  selectedCandidate?.confidenceLevel === 'Medium' ? 'text-amber-600 dark:text-amber-400' :
                  'text-red-600 dark:text-red-400'
                }`}>
                  {selectedCandidate ? `${(selectedCandidate.confidenceScore * 100).toFixed(0)}%` : 'N/A'}
                </span>
                <span className={`px-2 py-0.5 rounded-full text-white ${
                  selectedCandidate?.confidenceLevel === 'High' ? 'bg-green-600 dark:bg-green-700' :
                  selectedCandidate?.confidenceLevel === 'Medium' ? 'bg-amber-600 dark:bg-amber-700' :
                  'bg-red-600 dark:bg-red-700'
                }`}>
                  {selectedCandidate?.confidenceLevel || 'N/A'}
                </span>
              </div>

              {selectedCandidate && (
                <>
                  <div>
                    <span className="font-medium text-gray-700 dark:text-gray-300 block mb-1">Score Breakdown:</span>
                    <table className="w-full text-xs border-collapse">
                      <tbody>
                        {Object.entries(selectedCandidate.scoreBreakdown || {}).map(([key, value]) => (
                          <tr key={key} className="border-t border-gray-300 dark:border-gray-600">
                            <td className="py-1 pr-2 text-gray-700 dark:text-gray-300">{key}:</td>
                            <td className={`text-right font-medium ${value > 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
                              {value > 0 ? '+' : ''}{value.toFixed(2)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>

                  {selectedCandidate.matchedRules && selectedCandidate.matchedRules.length > 0 && (
                    <div>
                      <span className="font-medium text-gray-700 dark:text-gray-300">Matched Rules:</span>
                      <div className="ml-2 mt-1 space-y-1">
                        {selectedCandidate.matchedRules.map((rule, idx) => (
                          <div key={idx} className="text-gray-600 dark:text-gray-400 flex items-center gap-1">
                            <span className="text-green-600 dark:text-green-400">✓</span> {rule}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {selectedCandidate.missingRules && selectedCandidate.missingRules.length > 0 && (
                    <div>
                      <span className="font-medium text-gray-700 dark:text-gray-300">Missing Rules:</span>
                      <div className="ml-2 mt-1 space-y-1">
                        {selectedCandidate.missingRules.map((rule, idx) => (
                          <div key={idx} className="text-red-600 dark:text-red-400 flex items-center gap-1">
                            <span>✗</span> {rule}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          )}
        </div>

        {/* SECTION 3 - System Prompt Preview */}
        <div className="border-b border-gray-200 dark:border-gray-700">
          <button
            onClick={() => toggleSection('systemPrompt')}
            className="w-full px-4 py-3 flex items-center justify-between hover:bg-gray-100 dark:hover:bg-gray-800 transition text-left"
          >
            <span className="font-medium text-sm text-gray-900 dark:text-gray-100">
              System Prompt Preview
            </span>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {expandedSections.systemPrompt ? '▼' : '▶'}
            </span>
          </button>
          {expandedSections.systemPrompt && (
            <div className="px-4 py-3 bg-gray-50 dark:bg-gray-800/50 border-t border-gray-200 dark:border-gray-700">
              {systemPrompt ? (
                <>
                  <pre className="text-xs bg-gray-900 dark:bg-gray-950 text-gray-100 p-3 rounded overflow-x-auto mb-2 max-h-48 overflow-y-auto">
                    {systemPrompt}
                  </pre>
                  <button
                    onClick={() => copyToClipboard(systemPrompt)}
                    className="text-xs px-2 py-1 rounded bg-blue-600 hover:bg-blue-700 text-white transition"
                  >
                    Copy to clipboard
                  </button>
                </>
              ) : (
                <div className="text-xs text-gray-500 dark:text-gray-400">No system prompt available</div>
              )}
            </div>
          )}
        </div>

        {/* SECTION 4 - LLM Call Details */}
        <div className="border-b border-gray-200 dark:border-gray-700">
          <button
            onClick={() => toggleSection('llmDetails')}
            className="w-full px-4 py-3 flex items-center justify-between hover:bg-gray-100 dark:hover:bg-gray-800 transition text-left"
          >
            <span className="font-medium text-sm text-gray-900 dark:text-gray-100">
              LLM Call Details
            </span>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {expandedSections.llmDetails ? '▼' : '▶'}
            </span>
          </button>
          {expandedSections.llmDetails && (
            <div className="px-4 py-3 space-y-2 text-xs bg-gray-50 dark:bg-gray-800/50 border-t border-gray-200 dark:border-gray-700">
              <div>
                <span className="font-medium text-gray-700 dark:text-gray-300">Provider & Model:</span>
                <span className="ml-2 text-gray-600 dark:text-gray-400">Anthropic Claude Sonnet 4.6</span>
              </div>
              <div>
                <span className="font-medium text-gray-700 dark:text-gray-300">Input Tokens:</span>
                <span className="ml-2 text-gray-600 dark:text-gray-400 font-mono">{inputTokens}</span>
              </div>
              <div>
                <span className="font-medium text-gray-700 dark:text-gray-300">Output Tokens:</span>
                <span className="ml-2 text-gray-600 dark:text-gray-400 font-mono">{outputTokens}</span>
              </div>
              <div>
                <span className="font-medium text-gray-700 dark:text-gray-300">Total Tokens:</span>
                <span className="ml-2 text-gray-600 dark:text-gray-400 font-mono">{inputTokens + outputTokens}</span>
              </div>
              <div>
                <span className="font-medium text-gray-700 dark:text-gray-300">Response Time:</span>
                <span className="ml-2 text-gray-600 dark:text-gray-400">{responseTimeMs}ms</span>
              </div>
            </div>
          )}
        </div>

        {/* SECTION 5 - Guide References */}
        <div>
          <button
            onClick={() => toggleSection('guides')}
            className="w-full px-4 py-3 flex items-center justify-between hover:bg-gray-100 dark:hover:bg-gray-800 transition text-left"
          >
            <span className="font-medium text-sm text-gray-900 dark:text-gray-100">
              Guide References
            </span>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {expandedSections.guides ? '▼' : '▶'}
            </span>
          </button>
          {expandedSections.guides && (
            <div className="px-4 py-3 bg-gray-50 dark:bg-gray-800/50 border-t border-gray-200 dark:border-gray-700">
              {guideReferences && guideReferences.length > 0 ? (
                <div className="space-y-2 text-xs">
                  {guideReferences.map((guide, idx) => (
                    <div key={idx} className="text-gray-600 dark:text-gray-400 border-l-2 border-blue-400 pl-2">
                      {guide}
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-xs text-gray-500 dark:text-gray-400">No guide references</div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
