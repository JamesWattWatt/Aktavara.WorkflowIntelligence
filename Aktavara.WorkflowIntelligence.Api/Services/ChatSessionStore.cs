namespace Aktavara.WorkflowIntelligence.Api.Services;

using Aktavara.WorkflowIntelligence.Core.Models.Api;

/// <summary>
/// In-memory store for chat sessions with expiration.
/// </summary>
public class ChatSessionStore
{
    private readonly Dictionary<string, ChatSession> _sessions = new();
    private readonly object _lockObject = new();
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(2);
    private readonly int _maxSessions = 50;
    private readonly ILogger<ChatSessionStore> _logger;

    public ChatSessionStore(ILogger<ChatSessionStore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create or get a session.
    /// </summary>
    public ChatSession GetOrCreateSession(string? sessionId = null)
    {
        lock (_lockObject)
        {
            if (!string.IsNullOrEmpty(sessionId) && _sessions.TryGetValue(sessionId, out var existing))
            {
                existing.LastActivityAt = DateTime.UtcNow;
                return existing;
            }

            // Create new session
            var newSession = new ChatSession { SessionId = Guid.NewGuid().ToString() };

            // Clean up expired sessions if at capacity
            if (_sessions.Count >= _maxSessions)
            {
                var expired = _sessions
                    .Where(kvp => DateTime.UtcNow - kvp.Value.LastActivityAt > _sessionTimeout)
                    .ToList();

                foreach (var kvp in expired)
                {
                    _sessions.Remove(kvp.Key);
                    _logger.LogInformation("Removed expired session: {SessionId}", kvp.Key);
                }
            }

            _sessions[newSession.SessionId] = newSession;
            _logger.LogInformation("Created new session: {SessionId}", newSession.SessionId);
            return newSession;
        }
    }

    /// <summary>
    /// Get a session by ID.
    /// </summary>
    public ChatSession? GetSession(string sessionId)
    {
        lock (_lockObject)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.LastActivityAt = DateTime.UtcNow;
                return session;
            }
            return null;
        }
    }

    /// <summary>
    /// Save a message to a session.
    /// </summary>
    public void AddMessage(string sessionId, ChatMessage message)
    {
        lock (_lockObject)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                message.Timestamp = DateTime.UtcNow;
                session.Messages.Add(message);
                session.LastActivityAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Remove a session.
    /// </summary>
    public void RemoveSession(string sessionId)
    {
        lock (_lockObject)
        {
            _sessions.Remove(sessionId);
            _logger.LogInformation("Removed session: {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// List all active sessions.
    /// </summary>
    public List<ChatSession> GetAllSessions()
    {
        lock (_lockObject)
        {
            return new List<ChatSession>(_sessions.Values);
        }
    }

    /// <summary>
    /// Get session count.
    /// </summary>
    public int GetSessionCount()
    {
        lock (_lockObject)
        {
            return _sessions.Count;
        }
    }
}
