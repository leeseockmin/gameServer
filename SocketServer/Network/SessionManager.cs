using System.Collections.Concurrent;

namespace SocketServer.Network
{
	/// <summary>
	/// 전체 세션 관리 - 세션 ID 발급, 세션 추가/제거, 브로드캐스트
	/// </summary>
	public class SessionManager
	{
		private static SessionManager _instance = new SessionManager();
		public static SessionManager Instance => _instance;

		private readonly ConcurrentDictionary<long, ClientSession> _sessions = new ConcurrentDictionary<long, ClientSession>();
		private long _sessionIdGenerator = 0;

		private SessionManager() { }

		public ClientSession CreateSession()
		{
			var session = new ClientSession();
			session.SessionId = Interlocked.Increment(ref _sessionIdGenerator);

			_sessions.TryAdd(session.SessionId, session);
			return session;
		}

		public void RemoveSession(ClientSession session)
		{
			_sessions.TryRemove(session.SessionId, out _);
		}

		public ClientSession? GetSession(long sessionId)
		{
			_sessions.TryGetValue(sessionId, out var session);
			return session;
		}

		public int SessionCount => _sessions.Count;

		/// <summary>
		/// 모든 접속 세션에 패킷 전송
		/// </summary>
		public void Broadcast(ArraySegment<byte> sendBuff)
		{
			foreach (var session in _sessions.Values)
			{
				session.Send(sendBuff);
			}
		}
	}
}
