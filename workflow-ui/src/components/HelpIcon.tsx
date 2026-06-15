interface HelpIconProps {
  helpKey: string;
  onOpen: (key: string) => void;
  className?: string;
}

export function HelpIcon({ helpKey, onOpen, className = '' }: HelpIconProps) {
  return (
    <button
      onClick={() => onOpen(helpKey)}
      className={`inline-flex items-center justify-center w-5 h-5 rounded-full border border-gray-400 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:border-blue-500 hover:text-blue-600 dark:hover:border-blue-400 dark:hover:text-blue-400 transition-colors cursor-help text-xs font-medium ${className}`}
      title="Help"
    >
      ?
    </button>
  );
}
