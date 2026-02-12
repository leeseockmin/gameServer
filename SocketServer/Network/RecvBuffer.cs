namespace SocketServer.Network
{
	/// <summary>
	/// 수신 버퍼 - 소켓에서 받은 데이터를 조립하는 순환 버퍼
	/// [---- readPos ---- writePos ---- bufferSize]
	/// </summary>
	public class RecvBuffer
	{
		private byte[] _buffer;
		private int _readPos;
		private int _writePos;

		public RecvBuffer(int bufferSize)
		{
			_buffer = new byte[bufferSize];
			_readPos = 0;
			_writePos = 0;
		}

		/// <summary>
		/// 아직 처리하지 않은 데이터 크기
		/// </summary>
		public int DataSize => _writePos - _readPos;

		/// <summary>
		/// 남은 쓰기 가능 공간
		/// </summary>
		public int FreeSize => _buffer.Length - _writePos;

		/// <summary>
		/// 읽을 데이터 영역 (readPos ~ writePos)
		/// </summary>
		public ArraySegment<byte> ReadSegment => new ArraySegment<byte>(_buffer, _readPos, DataSize);

		/// <summary>
		/// 쓸 수 있는 영역 (writePos ~ end)
		/// </summary>
		public ArraySegment<byte> WriteSegment => new ArraySegment<byte>(_buffer, _writePos, FreeSize);

		/// <summary>
		/// 데이터를 읽은 후 readPos 이동
		/// </summary>
		public bool OnRead(int numOfBytes)
		{
			if (numOfBytes > DataSize)
				return false;

			_readPos += numOfBytes;
			return true;
		}

		/// <summary>
		/// 데이터를 쓴 후 writePos 이동
		/// </summary>
		public bool OnWrite(int numOfBytes)
		{
			if (numOfBytes > FreeSize)
				return false;

			_writePos += numOfBytes;
			return true;
		}

		/// <summary>
		/// 버퍼 정리 - 남은 데이터를 앞으로 이동
		/// </summary>
		public void Clean()
		{
			int dataSize = DataSize;
			if (dataSize == 0)
			{
				_readPos = 0;
				_writePos = 0;
			}
			else
			{
				Array.Copy(_buffer, _readPos, _buffer, 0, dataSize);
				_readPos = 0;
				_writePos = dataSize;
			}
		}
	}
}
