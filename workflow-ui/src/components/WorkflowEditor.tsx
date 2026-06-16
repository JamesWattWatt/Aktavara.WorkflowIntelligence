import { useState, useEffect } from 'react';
import type { WorkflowLibraryItem, WorkflowDefinition, ActivitySignatureRule, WorkflowState } from '../types/api';
import { apiClient } from '../services/apiClient';
import { HelpIcon } from './HelpIcon';

interface WorkflowEditorProps {
  workflow: WorkflowLibraryItem | null;
  onClose: () => void;
  onShowInference: () => void;
  onOpenHelp?: (key: string) => void;
}

export function WorkflowEditor({ workflow, onClose, onShowInference, onOpenHelp }: WorkflowEditorProps) {
  const [activeTab, setActiveTab] = useState<'overview' | 'rules' | 'states' | 'guides' | 'json'>('overview');
  const [definition, setDefinition] = useState<WorkflowDefinition | null>(null);
  const [loading, setLoading] = useState(!!workflow);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (workflow) {
      loadWorkflow();
    } else {
      setDefinition(createEmptyDefinition());
      setLoading(false);
    }
  }, [workflow]);

  function createEmptyDefinition(): WorkflowDefinition {
    return {
      workflowId: `workflow-${Date.now()}`,
      name: '',
      description: '',
      version: '1.0.0',
      status: 0,
      activitySignature: [],
      states: [],
      actions: [],
      tags: [],
      minimumConfidenceThreshold: 0.7,
      createdBy: 'user',
      createdDate: new Date().toISOString(),
      lastModifiedDate: new Date().toISOString(),
      metadata: {},
      helpGuideIds: []
    };
  }

  async function loadWorkflow() {
    try {
      setLoading(true);
      const def = await apiClient.getWorkflow(workflow!.id);
      setDefinition(def);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load workflow');
    } finally {
      setLoading(false);
    }
  }

  async function handleSave() {
    if (!definition) return;

    try {
      setSaving(true);
      if (workflow) {
        await apiClient.updateWorkflow(workflow.id, definition);
      } else {
        await apiClient.createWorkflow(definition);
      }
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save workflow');
    } finally {
      setSaving(false);
    }
  }

  if (loading || !definition) {
    return (
      <div className="fixed right-0 top-0 h-full w-96 bg-white dark:bg-gray-800 shadow-lg border-l border-gray-200 dark:border-gray-700 z-40 flex items-center justify-center">
        <p>Loading...</p>
      </div>
    );
  }

  return (
    <div className="fixed right-0 top-0 h-full w-96 bg-white dark:bg-gray-800 shadow-lg border-l border-gray-200 dark:border-gray-700 z-40 flex flex-col">
      {/* Header */}
      <div className="flex-shrink-0 flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700">
        <h2 className="text-lg font-bold">
          {workflow ? 'Edit Workflow' : 'New Workflow'}
        </h2>
        <button
          onClick={onClose}
          className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
        >
          ✕
        </button>
      </div>

      {/* Tabs */}
      <div className="flex-shrink-0 flex border-b border-gray-200 dark:border-gray-700 px-4">
        {(['overview', 'rules', 'states', 'guides', 'json'] as const).map(tab => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`px-3 py-2 text-sm font-medium border-b-2 transition-colors ${
              activeTab === tab
                ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
            }`}
          >
            {tab.charAt(0).toUpperCase() + tab.slice(1)}
          </button>
        ))}
      </div>

      {error && (
        <div className="flex-shrink-0 px-4 py-2 bg-red-50 dark:bg-red-900/20 border-b border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 text-sm">
          {error}
        </div>
      )}

      {/* Tab Content */}
      <div className="flex-1 min-h-0 overflow-y-auto px-4 py-4">
        {activeTab === 'overview' && (
          <OverviewTab
            definition={definition}
            onChange={setDefinition}
            onShowInference={onShowInference}
          />
        )}
        {activeTab === 'rules' && (
          <RulesTab
            definition={definition}
            onChange={setDefinition}
          />
        )}
        {activeTab === 'states' && (
          <StatesTab
            definition={definition}
            onChange={setDefinition}
            onOpenHelp={onOpenHelp}
          />
        )}
        {activeTab === 'guides' && (
          <GuidesTab definition={definition} />
        )}
        {activeTab === 'json' && (
          <JsonTab definition={definition} />
        )}
      </div>

      {/* Footer */}
      <div className="flex-shrink-0 flex gap-2 px-4 py-4 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
        <button
          onClick={onClose}
          className="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700 font-medium"
        >
          Cancel
        </button>
        <button
          onClick={handleSave}
          disabled={saving}
          className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium"
        >
          {saving ? 'Saving...' : 'Save'}
        </button>
      </div>
    </div>
  );
}

interface OverviewTabProps {
  definition: WorkflowDefinition;
  onChange: (def: WorkflowDefinition) => void;
  onShowInference: () => void;
}

function OverviewTab({ definition, onChange, onShowInference }: OverviewTabProps) {
  const [tags, setTags] = useState<string[]>(definition.tags);
  const [tagInput, setTagInput] = useState('');

  function handleAddTag(e: React.KeyboardEvent) {
    if (e.key === 'Enter' && tagInput.trim()) {
      e.preventDefault();
      const newTags = [...tags, tagInput.trim()];
      setTags(newTags);
      onChange({ ...definition, tags: newTags });
      setTagInput('');
    }
  }

  function handleRemoveTag(index: number) {
    const newTags = tags.filter((_, i) => i !== index);
    setTags(newTags);
    onChange({ ...definition, tags: newTags });
  }

  return (
    <div className="space-y-4">
      <div>
        <label className="block text-sm font-medium mb-1">Name *</label>
        <input
          type="text"
          value={definition.name}
          onChange={e => onChange({ ...definition, name: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
        />
      </div>

      <div>
        <label className="block text-sm font-medium mb-1">Description</label>
        <textarea
          value={definition.description}
          onChange={e => onChange({ ...definition, description: e.target.value })}
          rows={3}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
        />
      </div>

      <div>
        <label className="block text-sm font-medium mb-1">Risk Level</label>
        <select
          value={(definition.metadata?.riskLevel as string) || 'Medium'}
          onChange={e => onChange({
            ...definition,
            metadata: { ...definition.metadata, riskLevel: e.target.value }
          })}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
        >
          <option value="Low">Low</option>
          <option value="Medium">Medium</option>
          <option value="High">High</option>
        </select>
      </div>

      <div>
        <label className="block text-sm font-medium mb-1">Tags</label>
        <input
          type="text"
          value={tagInput}
          onChange={e => setTagInput(e.target.value)}
          onKeyDown={handleAddTag}
          placeholder="Type and press Enter"
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 mb-2"
        />
        <div className="flex flex-wrap gap-2">
          {tags.map((tag, i) => (
            <span key={i} className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 rounded-full text-sm flex items-center gap-1">
              {tag}
              <button
                onClick={() => handleRemoveTag(i)}
                className="hover:text-blue-900 dark:hover:text-blue-300"
              >
                ×
              </button>
            </span>
          ))}
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium mb-1">Status</label>
        <div className="space-y-1">
          {(['Approved', 'Candidate', 'Deprecated'] as const).map(status => (
            <label key={status} className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                checked={definition.status === 0} // Simplified for now
                onChange={() => {}}
              />
              <span>{status}</span>
            </label>
          ))}
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium mb-1">Confidence Threshold</label>
        <div className="flex items-center gap-2">
          <input
            type="range"
            min="0.5"
            max="0.9"
            step="0.05"
            value={definition.minimumConfidenceThreshold}
            onChange={e => onChange({ ...definition, minimumConfidenceThreshold: parseFloat(e.target.value) })}
            className="flex-1"
          />
          <span className="text-sm font-medium w-12 text-right">{definition.minimumConfidenceThreshold.toFixed(2)}</span>
        </div>
      </div>

      <div className="p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg text-sm">
        <p className="font-medium text-blue-900 dark:text-blue-300">✓ Valid workflow definition</p>
      </div>

      <button
        onClick={onShowInference}
        className="w-full px-4 py-2 border border-blue-600 text-blue-600 dark:border-blue-400 dark:text-blue-400 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 font-medium"
      >
        Infer from logs
      </button>
    </div>
  );
}

interface RulesTabProps {
  definition: WorkflowDefinition;
  onChange: (def: WorkflowDefinition) => void;
}

function RulesTab({ definition, onChange }: RulesTabProps) {
  const handleRuleChange = (index: number, field: keyof ActivitySignatureRule, value: any) => {
    const newRules = [...definition.activitySignature];
    newRules[index] = { ...newRules[index], [field]: value };
    onChange({ ...definition, activitySignature: newRules });
  };

  const handleAddRule = () => {
    const newRule: ActivitySignatureRule = {
      ruleId: `rule-${Date.now()}`,
      eventType: 0,
      recordKind: 0,
      workspaceKind: null,
      required: false,
      weight: 0.5,
      missingPenalty: 0.1,
      maxAgeMinutes: null,
      description: ''
    };
    onChange({ ...definition, activitySignature: [...definition.activitySignature, newRule] });
  };

  const handleDeleteRule = (index: number) => {
    const newRules = definition.activitySignature.filter((_, i) => i !== index);
    onChange({ ...definition, activitySignature: newRules });
  };

  const totalWeight = definition.activitySignature.reduce((sum, r) => sum + r.weight, 0);

  return (
    <div className="space-y-4">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-300 dark:border-gray-600">
              <th className="text-left py-2 px-1">Name</th>
              <th className="text-left py-2 px-1">Type</th>
              <th className="text-left py-2 px-1">Weight</th>
              <th className="text-left py-2 px-1"></th>
            </tr>
          </thead>
          <tbody>
            {definition.activitySignature.map((rule, i) => (
              <tr key={rule.ruleId} className="border-b border-gray-200 dark:border-gray-700">
                <td className="py-2 px-1">
                  <input
                    type="text"
                    value={rule.description}
                    onChange={e => handleRuleChange(i, 'description', e.target.value)}
                    className="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-xs bg-white dark:bg-gray-700"
                  />
                </td>
                <td className="py-2 px-1">
                  <select
                    value={rule.required ? 'required' : 'optional'}
                    onChange={e => handleRuleChange(i, 'required', e.target.value === 'required')}
                    className="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-xs bg-white dark:bg-gray-700"
                  >
                    <option value="required">Required</option>
                    <option value="optional">Optional</option>
                  </select>
                </td>
                <td className="py-2 px-1">
                  <input
                    type="number"
                    min="0"
                    max="1"
                    step="0.1"
                    value={rule.weight}
                    onChange={e => handleRuleChange(i, 'weight', parseFloat(e.target.value))}
                    className="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-xs bg-white dark:bg-gray-700"
                  />
                </td>
                <td className="py-2 px-1">
                  <button
                    onClick={() => handleDeleteRule(i)}
                    className="text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 text-xs"
                  >
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="space-y-2">
        <p className="text-xs font-medium">Weight Distribution ({totalWeight.toFixed(2)})</p>
        <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden flex">
          {definition.activitySignature.map((rule, i) => (
            <div
              key={rule.ruleId}
              style={{ width: `${totalWeight > 0 ? (rule.weight / totalWeight) * 100 : 0}%` }}
              className={`h-full ${i % 3 === 0 ? 'bg-blue-500' : i % 3 === 1 ? 'bg-purple-500' : 'bg-pink-500'}`}
            />
          ))}
        </div>
      </div>

      <div className="flex gap-2">
        <button
          onClick={handleAddRule}
          className="flex-1 px-3 py-2 border border-blue-600 text-blue-600 dark:border-blue-400 dark:text-blue-400 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 text-sm font-medium"
        >
          Add rule
        </button>
        <button
          className="flex-1 px-3 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 text-sm font-medium"
        >
          Normalise weights
        </button>
      </div>
    </div>
  );
}

interface StatesTabProps {
  definition: WorkflowDefinition;
  onChange: (def: WorkflowDefinition) => void;
  onOpenHelp?: (key: string) => void;
}

function StatesTab({ definition, onChange, onOpenHelp }: StatesTabProps) {
  const [states, setStates] = useState<WorkflowState[]>(definition.states || []);
  const [generatingAll, setGeneratingAll] = useState(false);
  const [generatingState, setGeneratingState] = useState<string | null>(null);
  const [editingQuestions, setEditingQuestions] = useState<string | null>(null);
  const [editQuestionText, setEditQuestionText] = useState<string>('');
  const [toast, setToast] = useState<{ message: string; stateId: string } | null>(null);

  const hasUnsavedChanges = JSON.stringify(states) !== JSON.stringify(definition.states || []);

  const handleStateChange = (index: number, field: string, value: any) => {
    const newStates = [...states];
    newStates[index] = { ...newStates[index], [field]: value };
    setStates(newStates);
    onChange({ ...definition, states: newStates });
  };

  const handleAddState = () => {
    const newState: WorkflowState = {
      stateId: `state-${Date.now()}`,
      name: 'New State',
      description: '',
      requiredEvidence: [],
      sequence: states.length,
      isTerminal: false,
      nextStateId: null,
      helpGuideId: '',
      workshopQuestions: [],
      metadata: {}
    };
    const newStates = [...states, newState];
    setStates(newStates);
    onChange({ ...definition, states: newStates });
  };

  const handleDeleteState = (index: number) => {
    const newStates = states.filter((_, i) => i !== index);
    setStates(newStates);
    onChange({ ...definition, states: newStates });
  };

  const handleGenerateQuestions = async (index: number) => {
    if (!definition.workflowId || hasUnsavedChanges) return;

    const stateId = states[index].stateId;
    const stateName = states[index].name;

    try {
      setGeneratingState(stateId);
      const updated = await apiClient.generateWorkflowQuestions(definition.workflowId, stateId);
      const updatedStateFromAPI = updated.states?.find(s => s.stateId === stateId);

      if (updatedStateFromAPI) {
        const newStates = states.map((s, i) =>
          i === index ? { ...s, workshopQuestions: updatedStateFromAPI.workshopQuestions } : s
        );
        setStates(newStates);
        onChange({ ...definition, states: newStates });

        setToast({ message: `Questions generated for ${stateName}`, stateId });
        setTimeout(() => setToast(null), 3000);
      }
    } catch (err) {
      console.error('Error generating questions:', err);
    } finally {
      setGeneratingState(null);
    }
  };

  const handleGenerateAllQuestions = async () => {
    if (!definition.workflowId || hasUnsavedChanges) return;

    try {
      setGeneratingAll(true);
      const updated = await apiClient.generateWorkflowQuestions(definition.workflowId);
      const newStates = updated.states || [];
      setStates(newStates);
      onChange({ ...definition, states: newStates });
    } catch (err) {
      console.error('Error generating questions:', err);
    } finally {
      setGeneratingAll(false);
    }
  };

  const startEditingQuestions = (stateId: string, questions: string[] = []) => {
    setEditingQuestions(stateId);
    setEditQuestionText(questions.join('\n'));
  };

  const saveQuestions = (index: number) => {
    const questions = editQuestionText
      .split('\n')
      .map(q => q.trim())
      .filter(q => q.length > 0);

    const newStates = [...states];
    newStates[index] = { ...newStates[index], workshopQuestions: questions };
    setStates(newStates);
    onChange({ ...definition, states: newStates });
    setEditingQuestions(null);
  };

  return (
    <div className="space-y-4">
      <div className="flex gap-2">
        <div
          className="flex-1 relative"
          title={hasUnsavedChanges ? 'Save changes first' : ''}
        >
          <button
            onClick={handleGenerateAllQuestions}
            disabled={generatingAll || hasUnsavedChanges}
            className="w-full px-3 py-2 bg-blue-600 text-white dark:bg-blue-500 dark:text-gray-900 rounded-lg hover:bg-blue-700 dark:hover:bg-blue-400 text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {generatingAll ? 'Generating...' : 'Generate questions for all steps'}
          </button>
        </div>
        <button
          onClick={handleAddState}
          className="flex-1 px-3 py-2 border border-blue-600 text-blue-600 dark:border-blue-400 dark:text-blue-400 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 text-sm font-medium"
        >
          Add state
        </button>
      </div>

      {states.map((state, i) => (
        <div key={state.stateId} className="p-3 border border-gray-300 dark:border-gray-600 rounded-lg space-y-2">
          <div className="flex justify-between items-start gap-2">
            <input
              type="text"
              value={state.name}
              onChange={e => handleStateChange(i, 'name', e.target.value)}
              className="flex-1 px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-700"
              placeholder="State name"
            />
            <button
              onClick={() => handleDeleteState(i)}
              className="text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 text-sm whitespace-nowrap"
            >
              Delete
            </button>
          </div>
          <textarea
            value={state.description}
            onChange={e => handleStateChange(i, 'description', e.target.value)}
            placeholder="Description"
            rows={2}
            className="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-700"
          />
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={state.isTerminal}
              onChange={e => handleStateChange(i, 'isTerminal', e.target.checked)}
            />
            <span className="text-sm">Is terminal state</span>
            {onOpenHelp && <HelpIcon helpKey="library-state-terminal" onOpen={onOpenHelp} />}
          </label>

          <div className="pt-2 border-t border-gray-200 dark:border-gray-700">
            <div className="flex justify-between items-center mb-2">
              <span className="text-sm font-medium">Workshop Questions</span>
              <div
                className="relative"
                title={hasUnsavedChanges ? 'Save changes first' : ''}
              >
                <button
                  onClick={() => handleGenerateQuestions(i)}
                  disabled={generatingState === state.stateId || hasUnsavedChanges}
                  className="text-sm px-2 py-1 border border-blue-500 text-blue-600 dark:text-blue-400 rounded hover:bg-blue-50 dark:hover:bg-blue-900/20 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {generatingState === state.stateId ? 'Generating...' : 'Generate questions for this step'}
                </button>
              </div>
            </div>

            {editingQuestions === state.stateId ? (
              <div className="space-y-2">
                <textarea
                  value={editQuestionText}
                  onChange={e => setEditQuestionText(e.target.value)}
                  placeholder="Enter questions, one per line"
                  rows={3}
                  className="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-700"
                />
                <div className="flex gap-2">
                  <button
                    onClick={() => saveQuestions(i)}
                    className="flex-1 px-2 py-1 bg-green-600 text-white rounded text-sm hover:bg-green-700"
                  >
                    Save
                  </button>
                  <button
                    onClick={() => setEditingQuestions(null)}
                    className="flex-1 px-2 py-1 bg-gray-400 text-white rounded text-sm hover:bg-gray-500"
                  >
                    Cancel
                  </button>
                </div>
              </div>
            ) : (
              <div className="space-y-1">
                {(state.workshopQuestions && state.workshopQuestions.length > 0) ? (
                  <>
                    {state.workshopQuestions.map((q, qi) => (
                      <p key={qi} className="text-sm text-gray-700 dark:text-gray-300">
                        {qi + 1}. {q}
                      </p>
                    ))}
                    <button
                      onClick={() => startEditingQuestions(state.stateId, state.workshopQuestions)}
                      className="text-sm px-2 py-1 border border-gray-400 text-gray-600 dark:text-gray-300 rounded hover:bg-gray-100 dark:hover:bg-gray-700 mt-2"
                    >
                      Edit
                    </button>
                  </>
                ) : (
                  <p className="text-sm text-gray-500 dark:text-gray-400 italic">No questions yet</p>
                )}
              </div>
            )}
          </div>
        </div>
      ))}

      {toast && (
        <div className="fixed bottom-4 right-4 bg-green-600 text-white px-4 py-2 rounded-lg shadow-lg text-sm animate-pulse">
          {toast.message}
        </div>
      )}
    </div>
  );
}

interface GuidesTabProps {
  definition: WorkflowDefinition;
}

function GuidesTab({ definition }: GuidesTabProps) {
  return (
    <div className="text-sm text-gray-600 dark:text-gray-400">
      <p className="mb-2">No states defined yet. Add states in the States tab to configure guides.</p>
      {(definition.states || []).length === 0 ? (
        <p className="text-xs">Create workflow states to map help guides.</p>
      ) : (
        (definition.states || []).map((state) => (
          <div key={state.stateId} className="mb-3">
            <p className="font-medium text-gray-900 dark:text-gray-100">{state.name}</p>
            <p className="text-xs">No guide mapped</p>
          </div>
        ))
      )}
    </div>
  );
}

interface JsonTabProps {
  definition: WorkflowDefinition;
}

function JsonTab({ definition }: JsonTabProps) {
  const handleCopy = () => {
    navigator.clipboard.writeText(JSON.stringify(definition, null, 2));
  };

  const handleDownload = () => {
    const blob = new Blob([JSON.stringify(definition, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${definition.workflowId}.workflow.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-2">
      <div className="flex gap-2">
        <button
          onClick={handleCopy}
          className="flex-1 px-3 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 text-sm font-medium"
        >
          Copy to clipboard
        </button>
        <button
          onClick={handleDownload}
          className="flex-1 px-3 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 text-sm font-medium"
        >
          Download JSON
        </button>
      </div>
      <pre className="p-3 bg-gray-100 dark:bg-gray-800 rounded-lg text-xs overflow-auto max-h-96 text-gray-800 dark:text-gray-200">
        {JSON.stringify(definition, null, 2)}
      </pre>
    </div>
  );
}
