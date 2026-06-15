import { useState } from 'react';
import type { AnalyzeResponse } from '../types/api';

interface LogDropZoneProps {
  onResult: (result: AnalyzeResponse) => void;
  onError?: (error: Error) => void;
}

export const LogDropZone = ({ onResult, onError }: LogDropZoneProps) => {
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useState<HTMLInputElement | null>(null)[1];

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
    try {
      const { apiClient } = await import('../services/apiClient');
      const result = await apiClient.uploadLogFile(file);
      onResult(result);
    } catch (error) {
      const err = error instanceof Error ? error : new Error('Unknown error');
      onError?.(err);
    }
  };

  return (
    <div
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      onClick={() => document.getElementById('file-input')?.click()}
      className={`
        border-2 border-dashed rounded-lg p-8 text-center cursor-pointer
        transition-colors
        ${isDragging
          ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
          : 'border-gray-300 dark:border-gray-600 hover:border-gray-400'
        }
      `}
    >
      <div className="text-gray-600 dark:text-gray-400">
        <p className="text-lg font-medium">Drop a log file here or click to browse</p>
        <p className="text-sm mt-2">Supported format: .txt</p>
      </div>
      <input
        id="file-input"
        ref={fileInputRef as any}
        type="file"
        onChange={handleFileChange}
        accept=".txt"
        className="hidden"
      />
    </div>
  );
};
