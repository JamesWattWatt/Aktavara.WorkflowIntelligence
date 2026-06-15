import { useState, useEffect } from 'react';
import type { WorkflowLibraryItem, WorkflowDefinition } from '../types/api';
import { apiClient } from '../services/apiClient';
import { WorkflowEditor } from './WorkflowEditor';
import { InferenceModal } from './InferenceModal';
import { HelpIcon } from './HelpIcon';

interface LibraryPageProps {
  onOpenHelp?: (key: string) => void;
}

export function LibraryPage({ onOpenHelp }: LibraryPageProps) {
  const [workflows, setWorkflows] = useState<WorkflowLibraryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'Approved' | 'Candidate' | 'Deprecated'>('all');
  const [selectedTags, setSelectedTags] = useState<Set<string>>(new Set());
  const [editingWorkflow, setEditingWorkflow] = useState<WorkflowLibraryItem | null>(null);
  const [showEditor, setShowEditor] = useState(false);
  const [showInference, setShowInference] = useState(false);
  const [showImport, setShowImport] = useState(false);
  const [importFile, setImportFile] = useState<File | null>(null);

  useEffect(() => {
    loadLibrary();
  }, []);

  async function loadLibrary() {
    try {
      setLoading(true);
      const data = await apiClient.getWorkflowLibrary();
      setWorkflows(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load library');
    } finally {
      setLoading(false);
    }
  }

  const allTags = Array.from(
    new Set(workflows.flatMap(w => w.tags))
  ).sort();

  const filteredWorkflows = workflows.filter(w => {
    const matchesSearch = w.name.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = statusFilter === 'all' || w.status === statusFilter;
    const matchesTags = selectedTags.size === 0 || w.tags.some(tag => selectedTags.has(tag));
    return matchesSearch && matchesStatus && matchesTags;
  });

  const stats = {
    total: workflows.length,
    approved: workflows.filter(w => w.status === 'Approved').length,
    candidate: workflows.filter(w => w.status === 'Candidate').length,
    deprecated: workflows.filter(w => w.status === 'Deprecated').length
  };

  function toggleTag(tag: string) {
    const newTags = new Set(selectedTags);
    if (newTags.has(tag)) {
      newTags.delete(tag);
    } else {
      newTags.add(tag);
    }
    setSelectedTags(newTags);
  }

  async function handleNewWorkflow() {
    setEditingWorkflow(null);
    setShowEditor(true);
  }

  function handleEditWorkflow(workflow: WorkflowLibraryItem) {
    setEditingWorkflow(workflow);
    setShowEditor(true);
  }

  async function handleImport() {
    if (!importFile) return;

    try {
      const text = await importFile.text();
      const definition = JSON.parse(text) as WorkflowDefinition;
      await apiClient.createWorkflow(definition);
      setImportFile(null);
      setShowImport(false);
      loadLibrary();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to import workflow');
    }
  }

  async function handleExportAll() {
    try {
      const approved = workflows.filter(w => w.status === 'Approved');
      if (approved.length === 0) {
        setError('No approved workflows to export');
        return;
      }

      const JSZip = (await import('jszip')).default;
      const zip = new JSZip();

      for (const workflow of approved) {
        const definition = await apiClient.getWorkflow(workflow.id);
        zip.file(
          `${workflow.id}.workflow.json`,
          JSON.stringify(definition, null, 2)
        );
      }

      const blob = await zip.generateAsync({ type: 'blob' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `aktavara-workflows-${new Date().toISOString().split('T')[0]}.zip`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to export workflows');
    }
  }

  if (loading) {
    return <div className="p-6 text-center">Loading library...</div>;
  }

  return (
    <div className="h-full flex flex-col bg-white dark:bg-gray-900">
      {/* Stats Row */}
      <div className="flex-shrink-0 px-6 pt-6 pb-4 border-b border-gray-200 dark:border-gray-800">
        <div className="flex gap-3">
          <div className="px-3 py-1 bg-gray-100 dark:bg-gray-800 rounded-full text-sm font-medium">
            {stats.total} workflows
          </div>
          <div className="px-3 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded-full text-sm font-medium">
            {stats.approved} approved
          </div>
          <div className="px-3 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 rounded-full text-sm font-medium">
            {stats.candidate} candidate
          </div>
          <div className="px-3 py-1 bg-gray-300 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-full text-sm font-medium">
            {stats.deprecated} deprecated
          </div>
        </div>
      </div>

      {/* Filter Bar & Actions */}
      <div className="flex-shrink-0 px-6 py-4 border-b border-gray-200 dark:border-gray-800 space-y-4">
        <div className="flex gap-3">
          <input
            type="text"
            placeholder="Search workflows..."
            value={searchTerm}
            onChange={e => setSearchTerm(e.target.value)}
            className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          />
          <select
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value as any)}
            className="px-3 py-2 border border-gray-300 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          >
            <option value="all">All statuses</option>
            <option value="Approved">Approved</option>
            <option value="Candidate">Candidate</option>
            <option value="Deprecated">Deprecated</option>
          </select>
        </div>

        {allTags.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {allTags.map(tag => (
              <button
                key={tag}
                onClick={() => toggleTag(tag)}
                className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${
                  selectedTags.has(tag)
                    ? 'bg-blue-600 text-white'
                    : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
                }`}
              >
                {tag}
              </button>
            ))}
          </div>
        )}

        <div className="flex gap-2">
          <button
            onClick={handleNewWorkflow}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-medium"
          >
            New workflow
          </button>
          <button
            onClick={() => setShowImport(true)}
            className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-900 dark:text-gray-100 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 font-medium"
          >
            Import JSON
          </button>
          <button
            onClick={handleExportAll}
            className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-900 dark:text-gray-100 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 font-medium"
          >
            Export all
          </button>
        </div>
      </div>

      {error && (
        <div className="px-6 py-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 text-sm m-4 rounded-lg">
          {error}
        </div>
      )}

      {/* Workflows Table or Empty State */}
      {filteredWorkflows.length === 0 ? (
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center text-gray-500 dark:text-gray-400">
            <p className="text-lg font-medium">No workflows in library</p>
            <p className="text-sm mt-2">Upload a log file in Discovery to get started</p>
          </div>
        </div>
      ) : (
        <div className="flex-1 min-h-0 overflow-auto">
          <table className="w-full text-sm">
            <thead className="sticky top-0 bg-gray-50 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
              <tr>
                <th className="px-4 py-3 text-left font-semibold text-gray-900 dark:text-gray-100">Name</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-900 dark:text-gray-100">Status</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-900 dark:text-gray-100">Risk</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-900 dark:text-gray-100">Rules</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-900 dark:text-gray-100">States</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-900 dark:text-gray-100">Modified</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-900 dark:text-gray-100">
                  <div className="flex items-center gap-2">
                    Actions
                    {onOpenHelp && <HelpIcon helpKey="library-edit" onOpen={onOpenHelp} />}
                  </div>
                </th>
              </tr>
            </thead>
            <tbody>
              {filteredWorkflows.map(workflow => (
                <tr key={workflow.id} className="border-b border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800/50">
                  <td className="px-4 py-3 font-bold text-gray-900 dark:text-gray-100">{workflow.name}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                      workflow.status === 'Approved' ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' :
                      workflow.status === 'Candidate' ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400' :
                      'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300'
                    }`}>
                      {workflow.status}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-sm font-medium ${
                      workflow.riskLevel === 'Low' ? 'text-green-600 dark:text-green-400' :
                      workflow.riskLevel === 'Medium' ? 'text-amber-600 dark:text-amber-400' :
                      'text-red-600 dark:text-red-400'
                    }`}>
                      {workflow.riskLevel}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{workflow.ruleCount}</td>
                  <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{workflow.stateCount}</td>
                  <td className="px-4 py-3 text-gray-600 dark:text-gray-400">
                    {new Date(workflow.lastModified).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 flex gap-2">
                    <button
                      onClick={() => handleEditWorkflow(workflow)}
                      className="text-blue-600 dark:text-blue-400 hover:underline text-sm font-medium"
                    >
                      Edit
                    </button>
                    <button
                      className="text-gray-600 dark:text-gray-400 hover:underline text-sm font-medium"
                    >
                      JSON
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Import Modal */}
      {showImport && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 max-w-md w-full">
            <h2 className="text-lg font-bold mb-4">Import Workflow</h2>
            <input
              type="file"
              accept=".workflow.json"
              onChange={e => setImportFile(e.target.files?.[0] || null)}
              className="w-full mb-4"
            />
            <div className="flex gap-2 justify-end">
              <button
                onClick={() => {
                  setShowImport(false);
                  setImportFile(null);
                }}
                className="px-4 py-2 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
              >
                Cancel
              </button>
              <button
                onClick={handleImport}
                disabled={!importFile}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
              >
                Import
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Editor Drawer */}
      {showEditor && (
        <WorkflowEditor
          workflow={editingWorkflow}
          onClose={() => {
            setShowEditor(false);
            setEditingWorkflow(null);
            loadLibrary();
          }}
          onShowInference={() => {
            setShowEditor(false);
            setShowInference(true);
          }}
        />
      )}

      {/* Inference Modal */}
      {showInference && (
        <InferenceModal
          onClose={() => setShowInference(false)}
          onApplySuggestions={() => {
            setShowInference(false);
            setShowEditor(true);
            loadLibrary();
          }}
        />
      )}
    </div>
  );
}
