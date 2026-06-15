import { useState, useRef } from 'react';
import type { AnalyzeResponse } from '../types/api';

interface LogDropZoneProps {
  onResult: (result: AnalyzeResponse) => void;
  onError?: (error: Error) => void;
}

const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB
const ALLOWED_TYPES = ['.txt'];

export const LogDropZone = ({ onResult, onError }: LogDropZoneProps) => {
  const [isDragging, setIsDragging] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const validateFile = (file: File): string | null => {
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!ALLOWED_TYPES.includes(ext)) {
      return `Invalid file type. Only .txt files are supported.`;
    }
    if (file.size > MAX_FILE_SIZE) {
      return `File is too large (${(file.size / 1024 / 1024).toFixed(1)}MB). Maximum size is 10MB.`;
    }
    return null;
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = () => {
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);

    const files = e.dataTransfer.files;
    if (files.length > 0) {
      handleFile(files[0]);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      handleFile(e.target.files[0]);
    }
  };

  const handleFile = async (file: File) => {
    setError(null);

    const validationError = validateFile(file);
    if (validationError) {
      setError(validationError);
      return;
    }

    setSelectedFile(file);
    setIsLoading(true);

    try {
      const { apiClient } = await import('../services/apiClient');
      const result = await apiClient.uploadLogFile(file);
      onResult(result);
      setSelectedFile(null);
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Unknown error');
      let friendlyMessage = error.message;

      if (error.message.includes('Failed to fetch') || error.message.includes('fetch')) {
        friendlyMessage = 'Cannot connect to the Workflow Intelligence API. Make sure it is running on http://localhost:5112';
      }

      setError(friendlyMessage);
      onError?.(new Error(friendlyMessage));
    } finally {
      setIsLoading(false);
    }
  };

  const handleClear = () => {
    setSelectedFile(null);
    setError(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <div className="space-y-2">
      <div
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={() => !isLoading && fileInputRef.current?.click()}
        className={`
          border-2 border-dashed rounded-lg p-8 text-center
          transition-all cursor-pointer relative
          ${isDragging
            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
            : 'border-gray-300 dark:border-gray-600 hover:border-gray-400'
          }
          ${isLoading ? 'opacity-75' : ''}
        `}
      >
        {isLoading ? (
          <div className="space-y-3">
            <div className="flex justify-center">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
            </div>
            <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
              Uploading and analyzing...
            </p>
          </div>
        ) : selectedFile ? (
          <div className="space-y-2">
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
              📄 {selectedFile.name}
            </p>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {(selectedFile.size / 1024).toFixed(1)} KB
            </p>
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleClear();
              }}
              className="text-xs text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 mt-2"
            >
              ✕ Clear
            </button>
          </div>
        ) : (
          <div className="text-gray-600 dark:text-gray-400">
            <p className="text-lg font-medium">Drop a log file here or click to browse</p>
            <p className="text-sm mt-2">Supported format: .txt (max 10MB)</p>
          </div>
        )}
        <input
          ref={fileInputRef}
          id="file-input"
          type="file"
          onChange={handleFileChange}
          accept=".txt"
          disabled={isLoading}
          className="hidden"
        />
      </div>

      {error && (
        <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-sm text-red-600 dark:text-red-400">
          ⚠ {error}
        </div>
      )}
    </div>
  );
};
