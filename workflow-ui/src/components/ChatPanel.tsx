import React, { useState, useEffect, useRef } from 'react';
import { marked } from 'marked';
import type { AnalyzeResponse, ChatMessage, ChatResponse, ChatSession } from '../types/api';
import { apiClient } from '../services/apiClient';

interface ChatPanelProps {
  sessionId: string | null;
  analyzeResponse: AnalyzeResponse | null;
  logFileName: string | null;
  onSessionCreated: (sessionId: string) => void;
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
  onSessionCreated
}) => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [session, setSession] = useState<ChatSession | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const currentState = analyzeResponse?.currentState || 'Unknown';
  const questionsToShow = analyzeResponse
    ? suggestedQuestions.contextual[currentState as keyof typeof suggestedQuestions.contextual] || suggestedQuestions.generic
    : suggestedQuestions.generic;

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = Math.min(textareaRef.current.scrollHeight, 96) + 'px';
    }
  }, [input]);

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
      setLoading(true);

      // Build context from analyzeResponse
      let logContent: string | undefined;
      if (analyzeResponse) {
        const topCandidate = analyzeResponse.workflowCandidates?.[0];
        const contextInfo = {
          contextNarrative: analyzeResponse.contextNarrative,
          detectedUser: analyzeResponse.detectedUser,
          fileName: analyzeResponse.fileName,
          currentState: analyzeResponse.currentState,
          topWorkflowCandidate: topCandidate ? {
            workflowId: topCandidate.workflowId,
            workflowName: topCandidate.workflowName,
            confidenceScore: topCandidate.confidenceScore,
            currentStateName: topCandidate.currentStateName
          } : null
        };
        logContent = JSON.stringify(contextInfo);
      }

      const request = {
        sessionId: session?.sessionId,
        message: text,
        logContent,
        userName: analyzeResponse?.detectedUser
      };

      const response: ChatResponse = await apiClient.sendChatMessage(request);

      // Create new session if this is the first message
      if (!session?.sessionId) {
        onSessionCreated(response.sessionId);
      }

      // Add user message
      const userMessage: ChatMessage = {
        role: 'user',
        content: text,
        timestamp: new Date().toISOString(),
        toolsUsed: []
      };

      // Add assistant message
      const assistantMessage: ChatMessage = {
        role: 'assistant',
        content: response.reply,
        timestamp: new Date().toISOString(),
        toolsUsed: response.toolsUsed || []
      };

      setMessages(prev => [...prev, userMessage, assistantMessage]);
      setInput('');

      // Reload session to get updated data
      const updatedSession = await apiClient.getChatSession(response.sessionId);
      setSession(updatedSession);
    } catch (err) {
      console.error('Failed to send message:', err);
    } finally {
      setLoading(false);
    }
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

  const toolsUsedCount = messages.reduce((acc, msg) => acc + msg.toolsUsed.length, 0);

  return (
    <div className="flex flex-col h-full bg-slate-800 border-l border-slate-700 rounded-lg">
      {/* Session Controls */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-slate-700 gap-2">
        <div className="text-xs text-slate-300">
          {messages.length} messages · {toolsUsedCount} tools
        </div>
        <div className="flex gap-1">
          <button
            onClick={handleNewConversation}
            className="px-2 py-1 text-xs rounded bg-slate-700 hover:bg-slate-600 text-slate-200 transition"
            title="Start new conversation"
          >
            New
          </button>
          <button
            onClick={handleSaveSession}
            disabled={!session}
            className="px-2 py-1 text-xs rounded bg-slate-700 hover:bg-slate-600 disabled:opacity-50 text-slate-200 transition"
            title="Save session to file"
          >
            Save
          </button>
          <button
            onClick={handleLoadSession}
            className="px-2 py-1 text-xs rounded bg-slate-700 hover:bg-slate-600 text-slate-200 transition"
            title="Load saved session"
          >
            Load
          </button>
        </div>
      </div>

      {/* Context Indicator */}
      <div className="px-4 py-2 border-b border-slate-700">
        {logFileName ? (
          <div className="text-xs text-slate-300 flex items-center gap-2">
            <span className="inline-block w-2 h-2 rounded-full bg-green-500"></span>
            Context: {logFileName}
          </div>
        ) : (
          <div className="text-xs text-slate-400 flex items-center gap-2">
            <span className="inline-block w-2 h-2 rounded-full bg-amber-500"></span>
            No log loaded — drop a file for context-aware responses
          </div>
        )}
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-3">
        {messages.length === 0 && (
          <div className="text-center text-slate-400 text-sm mt-8">
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
                  : 'bg-slate-700 text-slate-100'
              }`}
            >
              {msg.role === 'user' ? (
                <p className="whitespace-pre-wrap">{msg.content}</p>
              ) : (
                <div
                  className="prose prose-invert prose-sm max-w-none"
                  dangerouslySetInnerHTML={{
                    __html: renderMarkdown(msg.content)
                  }}
                  style={{
                    color: 'inherit',
                    '--tw-prose-body': 'rgb(226 232 240)',
                    '--tw-prose-headings': 'rgb(248 250 252)',
                    '--tw-prose-links': 'rgb(147 197 253)',
                    '--tw-prose-bold': 'rgb(248 250 252)',
                    '--tw-prose-code': 'rgb(226 232 240)',
                    '--tw-prose-bullets': 'rgb(203 213 225)',
                  } as React.CSSProperties}
                >
                  {/* Content rendered above */}
                </div>
              )}
              {msg.toolsUsed.length > 0 && (
                <div className="flex flex-wrap gap-1 mt-2">
                  {msg.toolsUsed.map(tool => (
                    <span
                      key={tool}
                      className="inline-block px-1.5 py-0.5 text-xs rounded bg-slate-600 text-slate-200"
                    >
                      [{tool}]
                    </span>
                  ))}
                </div>
              )}
              <p className="text-xs text-slate-300 mt-1">
                {new Date(msg.timestamp).toLocaleTimeString([], {
                  hour: '2-digit',
                  minute: '2-digit'
                })}
              </p>
            </div>
          </div>
        ))}
        {loading && (
          <div className="flex justify-start">
            <div className="bg-slate-700 text-slate-100 rounded-lg px-3 py-2">
              <span className="text-sm">typing...</span>
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Suggested Questions */}
      {messages.length === 0 && (
        <div className="px-4 py-3 border-b border-slate-700 border-t">
          <p className="text-xs text-slate-400 mb-2">Suggested questions:</p>
          <div className="flex flex-wrap gap-1">
            {questionsToShow.map(q => (
              <button
                key={q}
                onClick={() => handleSendMessage(q)}
                className="text-xs px-2 py-1 rounded-full bg-slate-700 hover:bg-slate-600 text-slate-300 hover:text-slate-200 transition whitespace-nowrap"
              >
                {q}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Input Area */}
      <div className="px-4 py-3 border-t border-slate-700">
        <div className="flex gap-2">
          <textarea
            ref={textareaRef}
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={e => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                handleSendMessage(input);
              }
            }}
            placeholder="Type your question..."
            className="flex-1 px-3 py-2 rounded bg-slate-700 text-slate-100 placeholder-slate-500 text-sm resize-none overflow-hidden max-h-24"
            rows={1}
            disabled={loading}
          />
          <button
            onClick={() => handleSendMessage(input)}
            disabled={!input.trim() || loading}
            className="px-3 py-2 rounded bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm font-medium transition"
          >
            Send
          </button>
        </div>
      </div>
    </div>
  );
};
