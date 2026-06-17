import React, { useState, useEffect, useRef } from 'react';
import { marked } from 'marked';
import type { AnalyzeResponse, ChatMessage, ChatSession } from '../types/api';
import { apiClient } from '../services/apiClient';
import { HelpIcon } from './HelpIcon';
import './ChatPanel.css';

interface ChatPanelProps {
  sessionId: string | null;
  analyzeResponse: AnalyzeResponse | null;
  logFileName: string | null;
  onSessionCreated: (sessionId: string) => void;
  onOpenHelp?: (key: string) => void;
}

// Configure marked for safe parsing
marked.setOptions({
  breaks: true,
  gfm: true
});

const renderMarkdown = (content: string): string => {
  try {
    return marked(content) as string;
  } catch {
    return content;
  }
};

const suggestedQuestions = {
  generic: [
    'What am I doing right now?',
    'What should I do next?',
    'How do I add a connector?',
    'Explain this workflow step'
  ],
  contextual: {
    'PathSaved': [
      'Did the path save correctly?',
      'What are the next steps?',
      'How do I verify the path?'
    ],
    'NodeModified': [
      'How do I save node changes?',
      'What properties should I configure?',
      'How do I validate the changes?'
    ],
    'ConnectorCreated': [
      'What connector properties should I set?',
      'How do I verify the connection?',
      'What is the next step?'
    ]
  }
};

export const ChatPanel: React.FC<ChatPanelProps> = ({
  sessionId,
  analyzeResponse,
  logFileName,
  onSessionCreated,
  onOpenHelp
}) => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [streaming, setStreaming] = useState(false);
  const [session, setSession] = useState<ChatSession | null>(null);
  const [panelWidth, setPanelWidth] = useState(360);
  const [isDragging, setIsDragging] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const dragStartX = useRef(0);
  const abortControllerRef = useRef<AbortController | null>(null);

  const currentState = analyzeResponse?.currentState || 'Unknown';
  const questionsToShow = analyzeResponse
    ? suggestedQuestions.contextual[currentState as keyof typeof suggestedQuestions.contextual] || suggestedQuestions.generic
    : suggestedQuestions.generic;

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  }, [messages]);

  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = Math.min(textareaRef.current.scrollHeight, 96) + 'px';
    }
  }, [input]);

  const handleDragStart = (e: React.MouseEvent) => {
    setIsDragging(true);
    dragStartX.current = e.clientX;
  };

  useEffect(() => {
    if (!isDragging) return;

    const handleMouseMove = (e: MouseEvent) => {
      if (!panelRef.current) return;
      const deltaX = e.clientX - dragStartX.current;
      const currentWidth = panelRef.current.offsetWidth;
      const newWidth = currentWidth - deltaX;

      if (newWidth >= 280 && newWidth <= 600) {
        setPanelWidth(newWidth);
        dragStartX.current = e.clientX;
      }
    };

    const handleMouseUp = () => {
      setIsDragging(false);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDragging]);

  useEffect(() => {
    if (sessionId) {
      apiClient.getChatSession(sessionId).then(s => {
        setSession(s);
        setMessages(s.messages);
      }).catch(err => console.error('Failed to load session:', err));
    }
  }, [sessionId]);

  const handleSendMessage = async (text: string) => {
    if (!text.trim()) return;

    try {
      setStreaming(true);
      setInput('');

      const topCandidate = analyzeResponse?.workflowCandidates?.[0];
      const currentSessionId = session?.sessionId;

      const request = {
        sessionId: currentSessionId,
        message: text,
        userName: analyzeResponse?.detectedUser,
        workflowContext: analyzeResponse?.contextNarrative,
        activeWorkflow: topCandidate ? {
          workflowId: topCandidate.workflowId,
          workflowName: topCandidate.workflowName,
          confidenceScore: topCandidate.confidenceScore,
          currentStateName: topCandidate.currentStateName,
          nextStepHint: topCandidate.nextStepHint
        } : undefined
      };

      // Add user message immediately
      const userMessage: ChatMessage = {
        role: 'user',
        content: text,
        timestamp: new Date().toISOString(),
        toolsUsed: []
      };

      // Create empty assistant message that will be filled by streaming
      const assistantMessage: ChatMessage = {
        role: 'assistant',
        content: '...',
        timestamp: new Date().toISOString(),
        toolsUsed: []
      };

      setMessages(prev => [...prev, userMessage, assistantMessage]);

      // Setup abort controller for cancellation
      abortControllerRef.current = new AbortController();

      // Stream response
      const response = await fetch('/api/chat/stream', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
        signal: abortControllerRef.current.signal
      });

      if (!response.body) throw new Error('Response body is empty');

      let finalSessionId = currentSessionId;
      let firstToken = true;
      let streamContent = '';

      const reader = response.body.getReader();
      const decoder = new TextDecoder();

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const chunk = decoder.decode(value, { stream: true });
        const lines = chunk.split('\n');

        for (const line of lines) {
          if (!line.startsWith('data: ')) continue;

          try {
            const data = JSON.parse(line.substring(6));

            if (firstToken && data.delta) {
              firstToken = false;
            }

            if (data.delta) {
              streamContent += data.delta;
              setMessages(prev =>
                prev.map((msg, idx) =>
                  idx === prev.length - 1
                    ? { ...msg, content: streamContent + ' ▊' }
                    : msg
                )
              );
            }

            if (data.done) {
              finalSessionId = data.sessionId;
              setMessages(prev =>
                prev.map((msg, idx) =>
                  idx === prev.length - 1
                    ? { ...msg, content: streamContent }
                    : msg
                )
              );
            }
          } catch (e) {
            // Ignore JSON parse errors for incomplete chunks
          }
        }
      }

      // Create new session if this is the first message
      if (!currentSessionId && finalSessionId) {
        onSessionCreated(finalSessionId);
        setSession({ sessionId: finalSessionId } as ChatSession);
      } else if (finalSessionId) {
        // Reload session to get updated data
        const updatedSession = await apiClient.getChatSession(finalSessionId);
        setSession(updatedSession);
      }
    } catch (err) {
      if ((err as Error).name !== 'AbortError') {
        console.error('Failed to send message:', err);
        setMessages(prev => prev.slice(0, -1)); // Remove the incomplete message
      }
    } finally {
      setStreaming(false);
      abortControllerRef.current = null;
    }
  };

  const handleCancelStream = () => {
    abortControllerRef.current?.abort();
    setStreaming(false);
  };

  const handleNewConversation = async () => {
    if (session?.sessionId) {
      try {
        await apiClient.deleteChatSession(session.sessionId);
      } catch (err) {
        console.error('Failed to delete session:', err);
      }
    }
    setSession(null);
    setMessages([]);
    setInput('');
  };

  const handleSaveSession = async () => {
    if (!session?.sessionId) return;
    try {
      await apiClient.saveChatSession(session.sessionId);
      // Trigger download
      const element = document.createElement('a');
      element.setAttribute('href', `data:text/json;charset=utf-8,${encodeURIComponent(JSON.stringify(session, null, 2))}`);
      element.setAttribute('download', `chat-session-${session.sessionId}.json`);
      element.style.display = 'none';
      document.body.appendChild(element);
      element.click();
      document.body.removeChild(element);
    } catch (err) {
      console.error('Failed to save session:', err);
    }
  };

  const handleLoadSession = () => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.onchange = async (e: any) => {
      const file = e.target.files?.[0];
      if (file) {
        try {
          const text = await file.text();
          const loadedSession = JSON.parse(text);
          setSession(loadedSession);
          setMessages(loadedSession.messages);
        } catch (err) {
          console.error('Failed to load session:', err);
        }
      }
    };
    input.click();
  };

  return (
    <div
      ref={panelRef}
      className="flex flex-col h-full bg-white dark:bg-gray-900 border-l border-t border-gray-200 dark:border-gray-700 rounded-lg relative"
      style={{ width: `${panelWidth}px` }}
    >
      {/* Drag Handle */}
      <div
        onMouseDown={handleDragStart}
        className={`absolute left-0 top-0 h-full w-1 cursor-col-resize transition-colors ${
          isDragging ? 'bg-blue-500' : 'bg-transparent hover:bg-blue-400'
        }`}
        title="Drag to resize chat panel"
      />
      {/* Chat Panel Header */}
      <div className="flex items-center justify-between px-3 py-2 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 min-h-[44px]">
        <div className="flex items-center gap-2 flex-shrink-0">
          <i className="ti ti-message-circle"
            aria-hidden="true"
            style={{ fontSize: '16px', color: '#2E75D1' }} />
          <span className="font-medium text-sm whitespace-nowrap text-gray-900 dark:text-gray-100">
            AI Assistant
          </span>
        </div>
        <div className="flex items-center gap-1 flex-shrink-0">
          {onOpenHelp && <HelpIcon helpKey="chat-panel" onOpen={onOpenHelp} />}
          <button
            onClick={handleNewConversation}
            className="px-2 py-1 text-xs rounded bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 text-gray-900 dark:text-gray-100 transition"
            title="Start new conversation"
          >
            New
          </button>
          <button
            onClick={handleSaveSession}
            disabled={!session}
            className="px-2 py-1 text-xs rounded bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 disabled:opacity-50 text-gray-900 dark:text-gray-100 transition"
            title="Save session to file"
          >
            Save
          </button>
          <button
            onClick={handleLoadSession}
            className="px-2 py-1 text-xs rounded bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 text-gray-900 dark:text-gray-100 transition"
            title="Load saved session"
          >
            Load
          </button>
        </div>
      </div>

      {/* Context Indicator */}
      <div className="px-4 py-2 border-b border-gray-200 dark:border-gray-700">
        {logFileName ? (
          <div className="text-xs text-gray-700 dark:text-gray-300 flex items-center gap-2">
            <span className="inline-block w-2 h-2 rounded-full bg-green-500"></span>
            Context: {logFileName}
          </div>
        ) : (
          <div className="text-xs text-gray-600 dark:text-gray-400 flex items-center gap-2">
            <span className="inline-block w-2 h-2 rounded-full bg-amber-500"></span>
            No log loaded — drop a file for context-aware responses
          </div>
        )}
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-3 min-h-0" style={{ overflowAnchor: 'none' }}>
        {messages.length === 0 && (
          <div className="text-center text-gray-500 dark:text-gray-400 text-sm mt-8">
            <p>Start a conversation</p>
          </div>
        )}
        {messages.map((msg, idx) => (
          <div
            key={idx}
            className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
          >
            <div
              className={`rounded-lg px-3 py-2 text-sm max-w-sm ${
                msg.role === 'user'
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100'
              }`}
            >
              {msg.role === 'user' ? (
                <p className="whitespace-pre-wrap">{msg.content}</p>
              ) : (
                <div
                  className="chat-message-prose text-gray-900 dark:text-gray-100"
                  dangerouslySetInnerHTML={{
                    __html: renderMarkdown(msg.content)
                  }}
                >
                  {/* Content rendered above */}
                </div>
              )}
              {msg.toolsUsed.length > 0 && (
                <div className="flex flex-wrap gap-1 mt-2">
                  {msg.toolsUsed.map(tool => (
                    <span
                      key={tool}
                      className={`inline-block px-1.5 py-0.5 text-xs rounded ${
                        msg.role === 'user'
                          ? 'bg-blue-700 text-blue-100'
                          : 'bg-gray-300 dark:bg-gray-700 text-gray-700 dark:text-gray-300'
                      }`}
                    >
                      [{tool}]
                    </span>
                  ))}
                </div>
              )}
              <p className={`text-xs mt-1 ${
                msg.role === 'user'
                  ? 'text-blue-100'
                  : 'text-gray-600 dark:text-gray-400'
              }`}>
                {new Date(msg.timestamp).toLocaleTimeString([], {
                  hour: '2-digit',
                  minute: '2-digit'
                })}
              </p>
            </div>
          </div>
        ))}
        {streaming && messages[messages.length - 1]?.role === 'user' && (
          <div className="flex justify-start">
            <div className="bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2">
              <span className="text-sm">...</span>
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Suggested Questions */}
      {messages.length === 0 && (
        <div className="px-4 py-3 border-b border-gray-200 dark:border-gray-700 border-t">
          <p className="text-xs text-gray-600 dark:text-gray-400 mb-2">Suggested questions:</p>
          <div className="flex flex-wrap gap-1">
            {questionsToShow.map(q => (
              <button
                key={q}
                onClick={() => handleSendMessage(q)}
                className="text-xs px-2 py-1 rounded-full bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 transition whitespace-nowrap"
              >
                {q}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Input Area */}
      <div className="px-4 py-3 border-t border-gray-200 dark:border-gray-700">
        <div className="flex gap-2">
          <textarea
            ref={textareaRef}
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={e => {
              if (e.key === 'Enter' && !e.shiftKey && !streaming) {
                e.preventDefault();
                handleSendMessage(input);
              }
            }}
            placeholder={streaming ? "Waiting for response..." : "Type your question..."}
            className="flex-1 px-3 py-2 rounded bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100 placeholder-gray-500 dark:placeholder-gray-400 text-sm resize-none overflow-hidden max-h-24 border border-gray-200 dark:border-gray-700"
            rows={1}
            disabled={streaming}
          />
          {streaming ? (
            <button
              type="button"
              onClick={handleCancelStream}
              className="px-3 py-2 rounded bg-red-600 hover:bg-red-700 text-white text-sm font-medium transition"
              title="Cancel streaming"
            >
              ✕
            </button>
          ) : (
            <button
              type="button"
              onClick={() => handleSendMessage(input)}
              disabled={!input.trim()}
              className="px-3 py-2 rounded bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm font-medium transition"
            >
              Send
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
