using System.Net;
using System.Net.Sockets;
using Serilog;

namespace SocketServer.Network
{
	/// <summary>
	/// IOCP 기반 Accept 리스너
	/// SocketAsyncEventArgs + AcceptAsync 비동기 패턴
	/// </summary>
	public class Listener
	{
		private Socket _listenSocket;
		private readonly SocketAsyncEventArgsPool _acceptArgsPool;
		private Func<ClientSession> _sessionFactory;

		public Listener(int backlog = 100)
		{
			_acceptArgsPool = new SocketAsyncEventArgsPool(backlog);
		}

		public void Start(IPEndPoint endPoint, Func<ClientSession> sessionFactory, int backlog = 100)
		{
			_sessionFactory = sessionFactory;

			_listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			_listenSocket.Bind(endPoint);
			_listenSocket.Listen(backlog);

			Log.Information($"[Listener] Listening on {endPoint}");

			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				RegisterAccept();
			}
		}

		private void RegisterAccept()
		{
			var acceptArgs = _acceptArgsPool.Pop();
			acceptArgs.Completed += OnAcceptCompleted;
			acceptArgs.AcceptSocket = null;

			try
			{
				bool pending = _listenSocket.AcceptAsync(acceptArgs);
				if (!pending)
					OnAcceptCompleted(null, acceptArgs);
			}
			catch (ObjectDisposedException)
			{
				return;
			}
		}

		private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs args)
		{
			if (args.SocketError == SocketError.Success && args.AcceptSocket != null)
			{
				var clientSocket = args.AcceptSocket;

				clientSocket.NoDelay = true;
				clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

				Log.Information($"[Listener] Client connected: {clientSocket.RemoteEndPoint}");

				var session = _sessionFactory.Invoke();
				session.Init(clientSocket);
			}
			else
			{
				Log.Error($"[Listener] Accept failed: {args.SocketError}");
			}

			args.Completed -= OnAcceptCompleted;
			_acceptArgsPool.Push(args);

			RegisterAccept();
		}

		public void Stop()
		{
			try
			{
				_listenSocket?.Close();
			}
			catch { }
		}
	}
}
