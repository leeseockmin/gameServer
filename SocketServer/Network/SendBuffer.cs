namespace SocketServer.Network
{
	/// <summary>
	/// 송신 버퍼 - 패킷 직렬화 후 전송할 데이터를 담는 버퍼
	/// ThreadLocal로 스레드별 독립 할당
	/// </summary>
	public class SendBuffer
	{
		private byte[] _buffer;
		private int _usedSize;

		public SendBuffer(int bufferSize)
		{
			_buffer = new byte[bufferSize];
			_usedSize = 0;
		}

		public int FreeSize => _buffer.Length - _usedSize;

		/// <summary>
		/// 쓸 수 있는 영역 반환
		/// </summary>
		public ArraySegment<byte> Open(int reserveSize)
		{
			if (reserveSize > FreeSize)
				return default;

			return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
		}

		/// <summary>
		/// 쓴 데이터를 확정하고 해당 세그먼트 반환
		/// </summary>
		public ArraySegment<byte> Close(int usedSize)
		{
			var segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
			_usedSize += usedSize;
			return segment;
		}
	}

	/// <summary>
	/// 스레드별 SendBuffer 관리
	/// </summary>
	public class SendBufferHelper
	{
		public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => null);

		public static int ChunkSize { get; set; } = 65535;

		public static ArraySegment<byte> Open(int reserveSize)
		{
			if (CurrentBuffer.Value == null)
				CurrentBuffer.Value = new SendBuffer(ChunkSize);

			if (CurrentBuffer.Value.FreeSize < reserveSize)
				CurrentBuffer.Value = new SendBuffer(ChunkSize);

			return CurrentBuffer.Value.Open(reserveSize);
		}

		public static ArraySegment<byte> Close(int usedSize)
		{
			return CurrentBuffer.Value.Close(usedSize);
		}
	}
}
