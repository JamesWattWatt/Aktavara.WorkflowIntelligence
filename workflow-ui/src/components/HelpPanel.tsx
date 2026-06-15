import { useEffect } from 'react';

interface HelpPanelProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  content: string;
}

function renderMarkdown(text: string): React.ReactNode {
  const lines = text.split('\n');
  const elements: React.ReactNode[] = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];

    if (line.startsWith('## ')) {
      elements.push(
        <h2 key={`h2-${i}`} className="text-lg font-bold text-gray-900 dark:text-gray-100 mt-4 mb-2">
          {line.slice(3)}
        </h2>
      );
      i++;
    } else if (line.startsWith('### ')) {
      elements.push(
        <h3 key={`h3-${i}`} className="text-base font-bold text-gray-900 dark:text-gray-100 mt-3 mb-2">
          {line.slice(4)}
        </h3>
      );
      i++;
    } else if (line.startsWith('- ')) {
      const listItems: string[] = [];
      while (i < lines.length && lines[i].startsWith('- ')) {
        listItems.push(lines[i].slice(2));
        i++;
      }
      elements.push(
        <ul key={`list-${i}`} className="list-disc list-inside space-y-1 text-sm text-gray-600 dark:text-gray-400 mb-2">
          {listItems.map((item, idx) => (
            <li key={idx} className="ml-2">
              {renderInlineMarkdown(item)}
            </li>
          ))}
        </ul>
      );
    } else if (line.startsWith('1. ') || line.match(/^\d+\. /)) {
      const listItems: string[] = [];
      while (i < lines.length && lines[i].match(/^\d+\. /)) {
        listItems.push(lines[i].replace(/^\d+\. /, ''));
        i++;
      }
      elements.push(
        <ol key={`ol-${i}`} className="list-decimal list-inside space-y-1 text-sm text-gray-600 dark:text-gray-400 mb-2">
          {listItems.map((item, idx) => (
            <li key={idx} className="ml-2">
              {renderInlineMarkdown(item)}
            </li>
          ))}
        </ol>
      );
    } else if (line === '---') {
      elements.push(<hr key={`hr-${i}`} className="my-4 border-gray-300 dark:border-gray-700" />);
      i++;
    } else if (line.trim() === '') {
      i++;
    } else {
      elements.push(
        <p key={`p-${i}`} className="text-sm text-gray-600 dark:text-gray-400 mb-2">
          {renderInlineMarkdown(line)}
        </p>
      );
      i++;
    }
  }

  return elements;
}

function renderInlineMarkdown(text: string): React.ReactNode {
  const parts: React.ReactNode[] = [];
  let lastIndex = 0;
  const boldRegex = /\*\*(.+?)\*\*/g;
  let match;

  while ((match = boldRegex.exec(text)) !== null) {
    if (match.index > lastIndex) {
      parts.push(text.substring(lastIndex, match.index));
    }
    parts.push(
      <strong key={`bold-${match.index}`} className="font-semibold text-gray-900 dark:text-gray-100">
        {match[1]}
      </strong>
    );
    lastIndex = match.index + match[0].length;
  }

  if (lastIndex < text.length) {
    parts.push(text.substring(lastIndex));
  }

  return parts.length > 0 ? parts : text;
}

export function HelpPanel({ isOpen, onClose, title, content }: HelpPanelProps) {
  useEffect(() => {
    function handleEscape(e: KeyboardEvent) {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    }

    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      return () => document.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, onClose]);

  return (
    <>
      {/* Overlay */}
      {isOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-40 transition-opacity"
          onClick={onClose}
        />
      )}

      {/* Sliding Panel */}
      <div
        className={`fixed right-0 top-0 h-full w-[380px] bg-white dark:bg-gray-800 shadow-lg border-l border-gray-200 dark:border-gray-700 z-50 overflow-y-auto transform transition-transform duration-300 ease-out ${
          isOpen ? 'translate-x-0' : 'translate-x-full'
        }`}
      >
        {/* Header */}
        <div className="sticky top-0 flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
          <h2 className="text-lg font-bold text-gray-900 dark:text-gray-100">{title}</h2>
          <button
            onClick={onClose}
            className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 text-2xl leading-none"
          >
            ×
          </button>
        </div>

        {/* Content */}
        <div className="px-6 py-4 space-y-2">
          {renderMarkdown(content)}
        </div>
      </div>
    </>
  );
}
