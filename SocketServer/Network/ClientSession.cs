using System.Net;
using System.Net.Sockets;
using Serilog;

namespace SocketServer.Network
{
	/// <summary>
	/// 클라이언트 세션 - 접속한 클라이언트 1명 = 1 세션
	/// SocketAsyncEventArgs 기반 비동기 Receive/Send
	/// </summary>
	public class ClientSession
	{
		public long SessionId { get; set; }
		public Socket Socket { get; private set; }
		public EndPoint RemoteEndPoint { get; private set; }

		private readonly RecvBuffer _recvBuffer = new RecvBuffer(65535);

		private readonly SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
		private readonly SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();

		private readonly Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
		private readonly object _sendLock = new object();
		private bool _isSending = false;

		private int _disconnected = 0;

		public Action<ClientSession, ArraySegment<byte>>? OnRecvPacket;
		public Action<ClientSession>? OnDisconnected;

		public void Init(Socket socket)
		{
			Socket = socket;
			RemoteEndPoint = socket.RemoteEndPoint!;
			_disconnected = 0;
			_isSending = false;

			_recvArgs.Completed += OnRecvCompleted;
			_sendArgs.Completed += OnSendCompleted;

			RegisterRecv();
		}

		#region Receive

		private void RegisterRecv()
		{
			if (_disconnected == 1)
				return;

			_recvBuffer.Clean();
			var segment = _recvBuffer.WriteSegment;
			_recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

			try
			{
				bool pending = Socket.ReceiveAsync(_recvArgs);
				if (pending == false)
					OnRecvCompleted(null, _recvArgs);
			}
			catch (ObjectDisposedException)
			{
				Disconnect();
			}
		}

		private void OnRecvCompleted(object? sender, SocketAsyncEventArgs args)
		{
			if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
			{
				if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
				{
					Disconnect();
					return;
				}

				int processLen = ProcessRecv();
				if (processLen < 0)
				{
					Disconnect();
					return;
				}

				RegisterRecv();
			}
			else
			{
				Disconnect();
			}
		}

		/// <summary>
		/// 수신 버퍼에서 완성된 패킷을 추출
		/// [PacketSize(4)] [PacketId(4)] [Body...]
		/// </summary>
		private int ProcessRecv()
		{
			int processLen = 0;

			while (true)
			{
				if (_recvBuffer.DataSize < PacketHeader.HeaderSize)
					break;

				int packetSize = PacketHeader.ReadPacketSize(_recvBuffer.ReadSegment);
				if (packetSize < PacketHeader.HeaderSize)
					return -1;

				if (_recvBuffer.DataSize < packetSize)
					break;

				var packetSegment = new ArraySegment<byte>(
					_recvBuffer.ReadSegment.Array!,
					_recvBuffer.ReadSegment.Offset,
					packetSize);

				OnRecvPacket?.Invoke(this, packetSegment);

				if (_recvBuffer.OnRead(packetSize) == false)
					return -1;

				processLen += packetSize;
			}

			return processLen;
		}

		#endregion

		#region Send

		public void Send(ArraySegment<byte> sendBuff)
		{
			lock (_sendLock)
			{
				_sendQueue.Enqueue(sendBuff);
				if (_isSending == false)
					RegisterSend();
			}
		}

		private void RegisterSend()
		{
			if (_disconnected == 1)
				return;

			_isSending = true;

			var list = new List<ArraySegment<byte>>();
			while (_sendQueue.Count > 0)
			{
				list.Add(_sendQueue.Dequeue());
			}

			_sendArgs.BufferList = list;

			try
			{
				bool pending = Socket.SendAsync(_sendArgs);
				if (pending == false)
					OnSendCompleted(null, _sendArgs);
			}
			catch (ObjectDisposedException)
			{
				Disconnect();
			}
		}

		private void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
		{
			lock (_sendLock)
			{
				if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
				{
					args.BufferList = null;

					if (_sendQueue.Count > 0)
						RegisterSend();
					else
						_isSending = false;
				}
				else
				{
					Disconnect();
				}
			}
		}

		#endregion

		#region Disconnect

		public void Disconnect()
		{
			if (Interlocked.Exchange(ref _disconnected, 1) == 1)
				return;

			Log.Information($"[Session] Disconnected: {RemoteEndPoint}, SessionId: {SessionId}");

			OnDisconnected?.Invoke(this);

			try
			{
				Socket.Shutdown(SocketShutdown.Both);
			}
			catch { }

			Socket.Close();
		}

		#endregion
	}
}
